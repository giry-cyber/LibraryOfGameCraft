using LibraryOfGamecraft.Dialogue;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryOfGamecraft.Editor.Dialogue
{
    public class DialogueGraphEditorWindow : EditorWindow
    {
        private DialogueGraphView _graphView;
        private DialogueSet _currentSet;
        private Label _titleLabel;

        [MenuItem("LibraryOfGamecraft/Dialogue Graph Editor")]
        public static void Open()
        {
            GetWindow<DialogueGraphEditorWindow>("Dialogue Graph");
        }

        public static void Open(DialogueSet set)
        {
            var window = GetWindow<DialogueGraphEditorWindow>("Dialogue Graph");
            window.LoadSet(set);
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

            _titleLabel = new Label("未選択") { style = { marginLeft = 8, marginRight = 8, unityFontStyleAndWeight = FontStyle.Bold } };
            toolbar.Add(_titleLabel);

            toolbar.Add(new ToolbarSpacer());

            toolbar.Add(new ToolbarButton(Save) { text = "保存" });
            toolbar.Add(new ToolbarButton(AutoLayout) { text = "自動整列" });
            toolbar.Add(new ToolbarButton(FrameAll) { text = "全体表示" });

            rootVisualElement.Add(toolbar);
        }

        private void CreateGraphView()
        {
            _graphView = new DialogueGraphView(this)
            {
                name = "Dialogue Graph",
                style = { flexGrow = 1 }
            };
            rootVisualElement.Add(_graphView);
        }

        public void LoadSet(DialogueSet set)
        {
            if (set == null) return;
            _currentSet = set;
            _titleLabel.text = $"{set.DisplayName}  ({set.DialogueSetId})";
            _graphView.PopulateGraph(set);
        }

        private void Save()
        {
            if (_currentSet == null)
            {
                Debug.LogWarning("[DialogueGraphEditor] 保存対象の DialogueSet がありません。");
                return;
            }
            EditorUtility.SetDirty(_currentSet);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DialogueGraphEditor] 保存しました: {_currentSet.DialogueSetId}");
        }

        private void AutoLayout()
        {
            _graphView?.AutoLayout();
        }

        private void FrameAll()
        {
            _graphView?.FrameAll();
        }
    }
}
