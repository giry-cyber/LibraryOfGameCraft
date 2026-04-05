using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace LibraryOfGamecraft.Building
{
    /// <summary>
    /// ProBuilder フェイスを解析して AnalyzedFace 群を生成し、
    /// 仕様書の法則で FaceSemantic を自動判定する。
    /// </summary>
    public static class FaceSemanticAnalyzer
    {
        // 法線による一次分類の閾値（仕様書 4.1）
        private const float UpwardThreshold   =  0.9f;
        private const float DownwardThreshold = -0.9f;
        private const float VerticalThreshold  =  0.2f;

        /// <summary>
        /// ProBuilderMesh を解析して AnalyzedFace のリストを返す。
        /// sourceId は解析サイクル内で一意な通し番号を採番する。
        /// </summary>
        public static List<AnalyzedFace> Analyze(ProBuilderMesh pbMesh)
        {
            var transform   = pbMesh.transform;
            var positions   = pbMesh.positions;
            var faces       = pbMesh.faces;

            // 建物全体の AABB 中心（ワールド空間）を求めておく（OuterWall 判定に使用）
            var worldBounds = ComputeWorldBounds(pbMesh);
            var buildingCenter = worldBounds.center;

            // Roof / Floor 判定用: UpwardFace の中で最大 Y を求める（仕様書 4.3）
            float maxFaceCenterY = float.MinValue;

            // 1st pass: 各面のワールド空間データを計算する
            var rawFaces = new List<(Face face, Vector3 center, Vector3 normal, float area, Bounds bounds, List<Vector3> verts)>();

            int idCounter = 0;

            foreach (var face in faces)
            {
                var distinctIdx = face.distinctIndexes;
                if (distinctIdx.Count < 3) continue;

                // ワールド頂点
                var worldVerts = new List<Vector3>(distinctIdx.Count);
                foreach (var idx in distinctIdx)
                    worldVerts.Add(transform.TransformPoint(positions[idx]));

                // 面中心
                var center = Vector3.zero;
                foreach (var v in worldVerts) center += v;
                center /= worldVerts.Count;

                // 面法線（三角形インデックスの最初のトライアングルから計算）
                var triIdx = face.indexes;
                var p0 = transform.TransformPoint(positions[triIdx[0]]);
                var p1 = transform.TransformPoint(positions[triIdx[1]]);
                var p2 = transform.TransformPoint(positions[triIdx[2]]);
                var normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;

                // 面積（全トライアングルの和）
                float area = 0f;
                for (int i = 0; i < triIdx.Count; i += 3)
                {
                    var a = transform.TransformPoint(positions[triIdx[i]]);
                    var b = transform.TransformPoint(positions[triIdx[i + 1]]);
                    var c = transform.TransformPoint(positions[triIdx[i + 2]]);
                    area += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                }

                // Bounds
                var faceBounds = new Bounds(worldVerts[0], Vector3.zero);
                for (int i = 1; i < worldVerts.Count; i++)
                    faceBounds.Encapsulate(worldVerts[i]);

                rawFaces.Add((face, center, normal, area, faceBounds, worldVerts));

                if (normal.y >= UpwardThreshold)
                    maxFaceCenterY = Mathf.Max(maxFaceCenterY, center.y);
            }

            // 2nd pass: Semantic を決定して AnalyzedFace を生成
            var result = new List<AnalyzedFace>(rawFaces.Count);

            foreach (var (face, center, normal, area, faceBounds, worldVerts) in rawFaces)
            {
                var analyzed = new AnalyzedFace
                {
                    sourceId      = ++idCounter,
                    faceCenter    = center,
                    faceNormal    = normal,
                    faceArea      = area,
                    bounds        = faceBounds,
                    worldVertices = worldVerts,
                    geometrySignature = GeometrySignature.Compute(center, normal, area, faceBounds.size)
                };

                ClassifySemantic(analyzed, buildingCenter, maxFaceCenterY, 0.05f);
                result.Add(analyzed);
            }

            return result;
        }

        private static void ClassifySemantic(
            AnalyzedFace face,
            Vector3 buildingCenter,
            float maxUpwardCenterY,
            float roofHeightEpsilon)
        {
            float ny = face.faceNormal.y;

            if (ny >= UpwardThreshold)
            {
                // Roof / Floor 判定（仕様書 4.3）
                if (face.faceCenter.y >= maxUpwardCenterY - roofHeightEpsilon)
                {
                    face.semantic   = FaceSemantic.Roof;
                    face.confidence = 1.0f;
                }
                else
                {
                    face.semantic   = FaceSemantic.Floor;
                    face.confidence = 1.0f;
                }
            }
            else if (ny <= DownwardThreshold)
            {
                // CeilingCandidate（仕様書 4.4）
                face.semantic   = FaceSemantic.CeilingCandidate;
                face.confidence = 1.0f;
            }
            else if (Mathf.Abs(ny) <= VerticalThreshold)
            {
                // OuterWall / InnerWallCandidate（仕様書 4.5）
                var dir = (face.faceCenter - buildingCenter).normalized;
                float dot = Vector3.Dot(face.faceNormal, dir);

                if (dot > 0f)
                {
                    face.semantic   = FaceSemantic.OuterWall;
                    face.confidence = 1.0f;
                }
                else
                {
                    face.semantic   = FaceSemantic.InnerWallCandidate;
                    face.confidence = 1.0f;
                }
            }
            else
            {
                // SlopedFace
                face.semantic   = FaceSemantic.SlopedFace;
                face.confidence = 0.5f;
            }
        }

        /// <summary>ProBuilderMesh の全頂点ワールド空間 AABB を求める</summary>
        public static Bounds ComputeWorldBounds(ProBuilderMesh pbMesh)
        {
            var transform = pbMesh.transform;
            var positions = pbMesh.positions;

            if (positions == null || positions.Count == 0)
                return new Bounds(transform.position, Vector3.zero);

            var b = new Bounds(transform.TransformPoint(positions[0]), Vector3.zero);
            for (int i = 1; i < positions.Count; i++)
                b.Encapsulate(transform.TransformPoint(positions[i]));
            return b;
        }

        /// <summary>AnalyzedFace から FaceSemanticRecord を生成する</summary>
        public static FaceSemanticRecord ToRecord(AnalyzedFace face)
        {
            return new FaceSemanticRecord
            {
                sourceId          = face.sourceId,
                semantic          = face.semantic,
                isManualOverride  = false,
                confidence        = face.confidence,
                geometrySignature = face.geometrySignature
            };
        }
    }
}
