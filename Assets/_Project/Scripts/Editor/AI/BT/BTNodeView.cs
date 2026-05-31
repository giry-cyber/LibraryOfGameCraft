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

        public void RemoveChildPortAt(int index)
        {
            if (index < 0 || index >= ChildPorts.Count) return;
            outputContainer.Remove(ChildPorts[index]);
            ChildPorts.RemoveAt(index);

            // 残りポートのインデックスを振り直す
            for (int i = index; i < ChildPorts.Count; i++)
            {
                ChildPorts[i].portName = $"[{i}]";
                ChildPorts[i].userData = i;
            }

            RefreshExpandedState();
            RefreshPorts();
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

        // ── ルート / ランタイムデバッグ表示 ──────────────────────
        private bool _isRoot;

        public void SetAsRoot(bool isRoot)
        {
            _isRoot = isRoot;
            ApplyBorder(isRoot ? Color.yellow : Color.clear, isRoot ? 2f : 0f);
        }

        public void UpdateStatus(BTStatus status)
        {
            var (color, width) = status switch
            {
                BTStatus.Running => (new Color(1f,   0.9f, 0f),    3f),
                BTStatus.Success => (new Color(0.2f, 0.9f, 0.2f),  2f),
                BTStatus.Failure => (new Color(0.9f, 0.2f, 0.2f),  2f),
                _                => (Color.clear,                   0f),
            };
            ApplyBorder(color, width);
        }

        public void ResetStatus()
        {
            ApplyBorder(_isRoot ? Color.yellow : Color.clear, _isRoot ? 2f : 0f);
        }

        private void ApplyBorder(Color color, float width)
        {
            style.borderTopColor    =
            style.borderBottomColor =
            style.borderLeftColor   =
            style.borderRightColor  = new StyleColor(color);
            style.borderTopWidth    =
            style.borderBottomWidth =
            style.borderLeftWidth   =
            style.borderRightWidth  = width;
        }
    }
}
