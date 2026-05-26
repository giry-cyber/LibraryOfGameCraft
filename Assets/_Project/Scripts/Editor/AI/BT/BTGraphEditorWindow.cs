using LibraryOfGamecraft.BT;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryOfGamecraft.BT.Editor
{
    public class BTGraphEditorWindow : EditorWindow
    {
        private BTGraphView _graphView;
        private BTGraph     _currentGraph;
        private Label       _titleLabel;

        [MenuItem("LibraryOfGamecraft/BT Graph Editor")]
        public static void Open()
        {
            GetWindow<BTGraphEditorWindow>("BT Graph");
        }

        public static void Open(BTGraph graph)
        {
            var window = GetWindow<BTGraphEditorWindow>("BT Graph");
            window.LoadGraph(graph);
        }

        // BTGraph アセットをダブルクリックで開く
        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is BTGraph graph)
            {
                Open(graph);
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            CreateToolbar();
            CreateGraphView();
        }

        private void OnDisable()
        {
            if (_graphView != null)
                rootVisualElement.Remove(_graphView);
        }

        private void CreateToolbar()
        {
            var toolbar = new Toolbar();

            _titleLabel = new Label("未選択")
            {
                style =
                {
                    marginLeft  = 8,
                    marginRight = 8,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            toolbar.Add(_titleLabel);
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(new ToolbarButton(Save)       { text = "保存" });
            toolbar.Add(new ToolbarButton(AutoLayout) { text = "自動整列" });
            toolbar.Add(new ToolbarButton(FrameAll)   { text = "全体表示" });

            rootVisualElement.Add(toolbar);
        }

        private void CreateGraphView()
        {
            _graphView = new BTGraphView { name = "BT Graph", style = { flexGrow = 1 } };
            rootVisualElement.Add(_graphView);
        }

        private void LoadGraph(BTGraph graph)
        {
            if (graph == null) return;
            _currentGraph = graph;
            _titleLabel.text = graph.name;
            _graphView.PopulateGraph(graph);
        }

        private void Save()
        {
            if (_currentGraph == null)
            {
                Debug.LogWarning("[BTGraphEditor] 保存対象の BTGraph がありません。");
                return;
            }
            EditorUtility.SetDirty(_currentGraph);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BTGraphEditor] 保存しました: {_currentGraph.name}");
        }

        private void AutoLayout() => _graphView?.AutoLayout();
        private void FrameAll()   => _graphView?.FrameAll();
    }
}
