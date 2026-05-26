using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryOfGamecraft.BT.Editor
{
    public class BTNodeView : Node
    {
        public BTNode BTNode { get; }
        public Port   InputPort  { get; private set; }
        public Port   OutputPort { get; private set; }          // Decorator 用（子1つ）
        public List<Port> ChildPorts { get; } = new();          // Composite 用（子N個）

        private readonly BTGraphView _graphView;

        public BTNodeView(BTNode node, BTGraphView graphView)
        {
            BTNode     = node;
            _graphView = graphView;
            title      = node.name;
            viewDataKey = node.GetInstanceID().ToString();

            SetPosition(new Rect(node.EditorPosition, Vector2.zero));
            CreatePorts();
            ApplyStyle();
        }

        // ── ポート生成 ────────────────────────────────────────
        private void CreatePorts()
        {
            // 親から接続される入力ポート（全ノード共通）
            InputPort = Port.Create<Edge>(Orientation.Vertical, Direction.Input,
                Port.Capacity.Single, typeof(BTNode));
            InputPort.portName = "";
            inputContainer.Add(InputPort);

            if (BTNode is BTComposite composite)
            {
                foreach (var _ in composite.Children)
                    AppendChildPort();

                var addBtn = new Button(OnAddChildClicked) { text = "＋ 子を追加" };
                extensionContainer.Add(addBtn);
            }
            else if (BTNode is BTDecorator)
            {
                OutputPort = Port.Create<Edge>(Orientation.Vertical, Direction.Output,
                    Port.Capacity.Single, typeof(BTNode));
                OutputPort.portName = "Child";
                outputContainer.Add(OutputPort);
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        public Port AppendChildPort()
        {
            var port = Port.Create<Edge>(Orientation.Vertical, Direction.Output,
                Port.Capacity.Single, typeof(BTNode));
            port.portName    = $"[{ChildPorts.Count}]";
            port.userData    = ChildPorts.Count;
            ChildPorts.Add(port);
            outputContainer.Add(port);
            return port;
        }

        private void OnAddChildClicked()
        {
            if (BTNode is BTComposite composite)
            {
                composite.Editor_AddChild(null);
                AppendChildPort();
                RefreshExpandedState();
                RefreshPorts();
            }
        }

        // ── 位置保存 ──────────────────────────────────────────
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            BTNode.EditorPosition = newPos.position;
        }

        // ── スタイル ──────────────────────────────────────────
        private void ApplyStyle()
        {
            var color = BTNode switch
            {
                BTComposite => new Color(0.15f, 0.35f, 0.55f),
                BTDecorator => new Color(0.35f, 0.15f, 0.55f),
                BTCondition => new Color(0.15f, 0.45f, 0.25f),
                _           => new Color(0.50f, 0.30f, 0.10f),   // BTAction
            };
            titleContainer.style.backgroundColor = new StyleColor(color);
        }

        // ── 選択時に Inspector と同期 ────────────────────────────
        public override void OnSelected()
        {
            base.OnSelected();
            UnityEditor.Selection.activeObject = BTNode;
        }

        // ── コンテキストメニュー ───────────────────────────────
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("ルートに設定", _ => _graphView.SetAsRoot(this));
            evt.menu.AppendSeparator();
            base.BuildContextualMenu(evt);
        }

        public void SetAsRoot(bool isRoot)
        {
            style.borderTopColor    =
            style.borderBottomColor =
            style.borderLeftColor   =
            style.borderRightColor  = new StyleColor(isRoot ? Color.yellow : Color.clear);
            style.borderTopWidth    =
            style.borderBottomWidth =
            style.borderLeftWidth   =
            style.borderRightWidth  = isRoot ? 2f : 0f;
        }
    }
}
