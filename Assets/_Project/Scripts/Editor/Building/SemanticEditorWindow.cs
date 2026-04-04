using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LibraryOfGamecraft.Building;

namespace LibraryOfGamecraft.Editor.Building
{
    /// <summary>
    /// 面 Semantic の一覧表示と手動修正を行う EditorWindow（仕様書 4.7）。
    /// Inspector の「Semantic Editor を開く」ボタンから起動する。
    /// </summary>
    public class SemanticEditorWindow : EditorWindow
    {
        private BuildingAuthoring _target;
        private Vector2 _scrollPos;

        // 再解析キャッシュ（表示のみ使用、Window を開くたびに更新）
        private List<AnalyzedFace> _cachedFaces;
        private bool _dirty;

        // ---- 静的エントリポイント ----

        public static void Open(BuildingAuthoring authoring)
        {
            var window = GetWindow<SemanticEditorWindow>("Semantic Editor");
            window.SetTarget(authoring);
            window.Show();
        }

        private void SetTarget(BuildingAuthoring authoring)
        {
            _target = authoring;
            Refresh();
        }

        // ---- ライフサイクル ----

        private void OnFocus() => Refresh();

        private void OnSelectionChange()
        {
            var selected = Selection.activeGameObject;
            if (selected != null)
            {
                var auth = selected.GetComponent<BuildingAuthoring>();
                if (auth != null && auth != _target)
                {
                    _target = auth;
                    Refresh();
                }
            }
        }

        private void Refresh()
        {
            _cachedFaces = null;
            _dirty = false;

            if (_target == null) return;

            var pbMesh = _target.GetComponent<UnityEngine.ProBuilder.ProBuilderMesh>();
            if (pbMesh == null) return;

            _cachedFaces = FaceSemanticAnalyzer.Analyze(pbMesh);
        }

        // ---- GUI ----

        private void OnGUI()
        {
            if (_target == null)
            {
                EditorGUILayout.HelpBox("BuildingAuthoring が選択されていません。", MessageType.Info);
                return;
            }

            // ヘッダー
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"対象: {_target.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (GUILayout.Button("リフレッシュ"))
                Refresh();

            if (_cachedFaces == null || _cachedFaces.Count == 0)
            {
                EditorGUILayout.HelpBox("ProBuilderMesh が見つからないか、フェイスがありません。", MessageType.Warning);
                return;
            }

            // SemanticStore から現在の record を引く
            var store     = _target.SemanticStore;
            var recordMap = new Dictionary<int, FaceSemanticRecord>();
            foreach (var r in store.Records)
                recordMap[r.sourceId] = r;

            EditorGUILayout.Space(4);

            // --- リスト ---
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var face in _cachedFaces)
            {
                recordMap.TryGetValue(face.sourceId, out var record);
                DrawFaceRow(face, record, store);
            }

            EditorGUILayout.EndScrollView();

            // --- 変更確定 ---
            if (_dirty)
            {
                EditorGUILayout.Space(4);
                if (GUILayout.Button("変更を適用 (Rebuild なし)", GUILayout.Height(28)))
                {
                    EditorUtility.SetDirty(_target.SemanticStore);
                    SceneView.RepaintAll();
                    _dirty = false;
                }
            }
        }

        private void DrawFaceRow(AnalyzedFace face, FaceSemanticRecord record, SemanticStore store)
        {
            if (record == null) return;

            var semantic    = record.semantic;
            float confidence = record.confidence;
            bool uncertain  = confidence < 0.75f;

            var baseColor = SemanticVisualizationDrawer.SemanticToColor(semantic);
            var labelStyle = new GUIStyle(EditorStyles.label);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                // カラーパッチ
                var colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
                EditorGUI.DrawRect(colorRect, baseColor);

                GUILayout.Space(4);

                // ID / 法線 / confidence
                string uncertainMark = uncertain ? " ⚠" : "";
                EditorGUILayout.LabelField(
                    $"ID:{face.sourceId}  N:{face.faceNormal:F2}  conf:{confidence:F2}{uncertainMark}",
                    GUILayout.Width(260));

                // Manual override マーク
                if (record.isManualOverride)
                {
                    var savedColor = GUI.color;
                    GUI.color = Color.yellow;
                    GUILayout.Label("手動", GUILayout.Width(32));
                    GUI.color = savedColor;
                }
                else
                {
                    GUILayout.Space(36);
                }

                // Semantic ドロップダウン
                EditorGUI.BeginChangeCheck();
                var newSemantic = (FaceSemantic)EditorGUILayout.EnumPopup(semantic, GUILayout.Width(160));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(store, "Change Face Semantic");
                    store.ApplyManualOverride(face.sourceId, newSemantic);
                    _dirty = true;
                    SceneView.RepaintAll();
                }
            }
        }
    }
}
