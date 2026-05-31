using LibraryOfGamecraft.AI;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryOfGamecraft.Editor.AI
{
    public class AIGraphEditorWindow : EditorWindow
    {
        private AIGraphView _graphView;
        private AIBehaviourGraph _currentGraph;
        private Label _titleLabel;

        [MenuItem("LibraryOfGamecraft/AI Graph Editor")]
        public static void Open()
        {
            GetWindow<AIGraphEditorWindow>("AI Graph");
        }

        public static void Open(AIBehaviourGraph graph)
        {
            var window = GetWindow<AIGraphEditorWindow>("AI Graph");
            window.LoadGraph(graph);
        }

        // AIBehaviourGraph アセットをダブルクリックで開く
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is AIBehaviourGraph graph)
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
                style = { marginLeft = 8, marginRight = 8, unityFontStyleAndWeight = FontStyle.Bold }
            };
            toolbar.Add(_titleLabel);
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(new ToolbarButton(Save) { text = "保存" });
            toolbar.Add(new ToolbarButton(AutoLayout) { text = "自動整列" });
            toolbar.Add(new ToolbarButton(FrameAll) { text = "全体表示" });

            rootVisualElement.Add(toolbar);
        }

        private void CreateGraphView()
        {
            _graphView = new AIGraphView(this) { name = "AI Graph", style = { flexGrow = 1 } };
            rootVisualElement.Add(_graphView);
        }

        public void LoadGraph(AIBehaviourGraph graph)
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
                Debug.LogWarning("[AIGraphEditor] 保存対象の AIBehaviourGraph がありません。");
                return;
            }
            EditorUtility.SetDirty(_currentGraph);
            foreach (var node in _currentGraph.Nodes)
                if (node != null) EditorUtility.SetDirty(node);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AIGraphEditor] 保存しました: {_currentGraph.name}");
        }

        private void AutoLayout() => _graphView?.AutoLayout();
        private void FrameAll() => _graphView?.FrameAll();
    }
}
