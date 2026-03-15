using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FigmaImporter
{
    /// <summary>
    /// Window > Figma Importer で開くエディタウィンドウ。
    /// Figma MCP / REST API から取得した JSON を貼り付けて Unity に配置する。
    /// </summary>
    public class FigmaImporterWindow : EditorWindow
    {
        private string _json = "";
        private Canvas _targetCanvas;
        private Vector2 _scroll;
        private string _status = "";

        [MenuItem("Window/Figma Importer")]
        public static void Open()
        {
            var win = GetWindow<FigmaImporterWindow>("Figma Importer");
            win.minSize = new Vector2(420, 300);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Figma → Unity uGUI Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // ターゲット Canvas
            _targetCanvas = (Canvas)EditorGUILayout.ObjectField(
                "Target Canvas", _targetCanvas, typeof(Canvas), true);

            if (_targetCanvas == null)
            {
                EditorGUILayout.HelpBox(
                    "シーン上の Canvas を指定してください。\n" +
                    "Canvas が無い場合は GameObject > UI > Canvas で作成できます。",
                    MessageType.Info);
            }

            EditorGUILayout.Space(6);

            // JSON 入力エリア
            EditorGUILayout.LabelField("Figma JSON (ファイルまたはノードの JSON を貼り付け)");
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(160));
            _json = EditorGUILayout.TextArea(_json, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);

            // ボタン行
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("JSON をクリア"))
            {
                _json = "";
                _status = "";
            }

            GUI.enabled = _targetCanvas != null && !string.IsNullOrWhiteSpace(_json);
            if (GUILayout.Button("インポート", GUILayout.Height(28)))
                RunImport();
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // ステータス
            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(_status, MessageType.Info);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("対応ノード: FRAME / GROUP / RECTANGLE / TEXT / COMPONENT / INSTANCE",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField("未対応ノード (VECTOR 等) は自動スキップされます。",
                EditorStyles.miniLabel);
        }

        private void RunImport()
        {
            _status = "";

            var file = FigmaNodeParser.ParseFile(_json);
            if (file == null)
            {
                _status = "JSON のパースに失敗しました。Console を確認してください。";
                return;
            }

            // Undo に登録
            Undo.RegisterFullObjectHierarchyUndo(_targetCanvas.gameObject, "Figma Import");

            FigmaToUnity.BuildFromFile(file, _targetCanvas);

            _status = $"インポート完了: {file.name}";
            Debug.Log($"[FigmaImporter] 完了: {file.name}");
        }
    }
}
