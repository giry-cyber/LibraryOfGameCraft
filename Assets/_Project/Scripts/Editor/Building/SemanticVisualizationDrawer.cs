using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using LibraryOfGamecraft.Building;

namespace LibraryOfGamecraft.Editor.Building
{
    /// <summary>
    /// BuildingAuthoring 選択時に Semantic オーバーレイを Scene ビューへ描画する（仕様書 4.6）。
    /// [DrawGizmo] 属性を使用して BuildingAuthoring 側に Editor 依存を持ち込まない。
    /// </summary>
    public static class SemanticVisualizationDrawer
    {
        private const float UncertainThreshold = 0.75f;
        private const float FaceAlpha          = 0.35f;
        private const float OutlineAlpha       = 0.9f;

        [DrawGizmo(GizmoType.Selected | GizmoType.Active, typeof(BuildingAuthoring))]
        private static void DrawBuildingGizmos(BuildingAuthoring authoring, GizmoType gizmoType)
        {
            if (!authoring.showSemanticOverlay) return;

            var store  = authoring.SemanticStore;
            if (store.Records.Count == 0) return;

            var pbMesh = authoring.GetComponent<ProBuilderMesh>();
            if (pbMesh == null) return;

            // 再解析してワールド空間頂点を取得（SemanticStore の record と sourceId で突合）
            var faces = FaceSemanticAnalyzer.Analyze(pbMesh);

            // sourceId → SemanticRecord のマップを構築
            var recordMap = new System.Collections.Generic.Dictionary<int, FaceSemanticRecord>();
            foreach (var r in store.Records)
                recordMap[r.sourceId] = r;

            foreach (var face in faces)
            {
                if (!recordMap.TryGetValue(face.sourceId, out var record)) continue;

                var baseColor = SemanticToColor(record.semantic);

                // 面の塗りつぶし
                DrawFilledFace(face, baseColor);

                // Uncertain アウトライン（confidence < 0.75 は Orange アウトライン、仕様書 4.6）
                if (record.confidence < UncertainThreshold)
                    DrawOutline(face, new Color(1f, 0.5f, 0f, OutlineAlpha));
            }
        }

        private static void DrawFilledFace(AnalyzedFace face, Color baseColor)
        {
            if (face.worldVertices.Count < 3) return;

            var fillColor = baseColor;
            fillColor.a = FaceAlpha;
            Handles.color = fillColor;

            // Fan 分割で凸多角形を描画
            var verts = face.worldVertices;
            int n = verts.Count;
            for (int i = 1; i < n - 1; i++)
            {
                Handles.DrawAAConvexPolygon(verts[0], verts[i], verts[i + 1]);
            }
        }

        private static void DrawOutline(AnalyzedFace face, Color color)
        {
            if (face.worldVertices.Count < 2) return;

            Handles.color = color;
            var verts = face.worldVertices;
            int n = verts.Count;
            for (int i = 0; i < n; i++)
            {
                Handles.DrawLine(verts[i], verts[(i + 1) % n], 2f);
            }
        }

        /// <summary>仕様書 4.6 の Semantic カラーテーブル</summary>
        public static Color SemanticToColor(FaceSemantic semantic)
        {
            return semantic switch
            {
                FaceSemantic.OuterWall          => Color.red,
                FaceSemantic.InnerWallCandidate => Color.magenta,
                FaceSemantic.Floor              => Color.green,
                FaceSemantic.Roof               => Color.blue,
                FaceSemantic.CeilingCandidate   => Color.cyan,
                FaceSemantic.SlopedFace         => Color.gray,
                FaceSemantic.OpeningHost        => Color.yellow,
                _                               => Color.white
            };
        }
    }
}
