using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    /// <summary>
    /// Floor / CeilingCandidate 面の簡易メッシュを生成する（仕様書 6.3）。
    /// </summary>
    public static class FloorCeilingGenerator
    {
        public static (GeneratedElement element, GameObject go) Generate(
            AnalyzedFace face,
            FloorCeilingSettings settings,
            SourceKind kind,
            Transform parent)
        {
            var go = new GameObject(kind == SourceKind.FloorCell ? "Floor" : "Ceiling");
            go.transform.SetParent(parent, false);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();

            if (settings.material != null)
                mr.sharedMaterial = settings.material;

            // 面法線方向のオフセットを適用した頂点群
            var normal = face.faceNormal;
            var offsetVerts = new List<Vector3>(face.worldVertices.Count);
            foreach (var v in face.worldVertices)
                offsetVerts.Add(v + normal * settings.offset);

            mf.sharedMesh = BuildPolygonMesh(offsetVerts, normal, settings.thickness);

            if (settings.generateCollider)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
            }

            var bounds = new Bounds(face.faceCenter + normal * settings.offset, Vector3.zero);
            foreach (var v in offsetVerts) bounds.Encapsulate(v);

            var element = new GeneratedElement
            {
                elementId       = GeneratedObjectRegistry.IssueElementId(),
                sourceKind      = kind,
                sourceId        = face.sourceId,
                role            = ModuleRole.PrimaryWall,   // Floor/Ceiling はロールなし（便宜上 PrimaryWall）
                moduleAssetId   = string.Empty,
                generationGroup = $"{kind}_{face.sourceId}",
                bounds          = bounds
            };

            return (element, go);
        }

        /// <summary>
        /// 凸多角形頂点から表裏両面の薄板メッシュを生成する。
        /// 頂点は Fan 展開でトライアングル化する（面が凸の直方体フェイスを想定）。
        /// </summary>
        private static Mesh BuildPolygonMesh(List<Vector3> worldVerts, Vector3 normal, float thickness)
        {
            int n = worldVerts.Count;
            var vertices  = new Vector3[n * 2];
            var normals   = new Vector3[n * 2];
            var uvs       = new Vector2[n * 2];

            // UV 投影軸
            var right   = Vector3.Cross(normal, Vector3.up);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(normal, Vector3.right);
            right.Normalize();
            var upAxis = Vector3.Cross(right, normal).normalized;

            for (int i = 0; i < n; i++)
            {
                vertices[i]     = worldVerts[i];
                vertices[i + n] = worldVerts[i] - normal * thickness;
                normals[i]      =  normal;
                normals[i + n]  = -normal;
                uvs[i]          = new Vector2(Vector3.Dot(worldVerts[i], right), Vector3.Dot(worldVerts[i], upAxis));
                uvs[i + n]      = uvs[i];
            }

            // Fan トライアングル（表面）
            var tris = new List<int>();
            for (int i = 1; i < n - 1; i++)
            {
                tris.Add(0); tris.Add(i); tris.Add(i + 1);         // 表
                tris.Add(n); tris.Add(n + i + 1); tris.Add(n + i); // 裏
            }

            var mesh = new Mesh
            {
                name     = "FloorCeilingMesh",
                vertices = vertices,
                normals  = normals,
                uv       = uvs
            };
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
