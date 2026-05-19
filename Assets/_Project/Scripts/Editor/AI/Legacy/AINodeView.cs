using System.Collections.Generic;
using LibraryOfGamecraft.AI;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryOfGamecraft.Editor.AI
{
    public class AINodeView : Node
    {
        public AINode AINode { get; }
        public Port InputPort { get; private set; }

        private readonly List<Port> _outputPorts = new();
        private readonly AIBehaviourGraph _graph;
        private readonly AIGraphView _graphView;

        private static readonly Color ColorDefault  = new(0.22f, 0.35f, 0.50f);
        private static readonly Color ColorStart    = new(0.60f, 0.45f, 0.10f);

        public AINodeView(AINode node, AIBehaviourGraph graph, AIGraphView graphView)
        {
            AINode    = node;
            _graph    = graph;
            _graphView = graphView;

            title = node.name;
            SetPosition(new Rect(node.Position, Vector2.zero));
            RefreshStartNodeStyle(graph.StartNode);

            CreateInputPort();
            for (int i = 0; i < node.Transitions.Count; i++)
                AppendOutputPort(i);
            CreateAddTransitionButton();

            RefreshExpandedState();
            RefreshPorts();
        }

        // ─────────────────────────────────────
        // 構築
        // ─────────────────────────────────────

        private void CreateInputPort()
        {
            InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);
        }

        private void AppendOutputPort(int index)
        {
            var t     = AINode.Transitions[index];
            var label = t.Condition != null ? t.Condition.name : "Always";

            var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = label;
            port.name     = $"transition_{index}";
            outputContainer.Add(port);
            _outputPorts.Add(port);
        }

        private void CreateAddTransitionButton()
        {
            var btn = new Button(() =>
            {
                AINode.AddEmptyTransition();
                AppendOutputPort(AINode.Transitions.Count - 1);
                RefreshExpandedState();
                RefreshPorts();
            })
            { text = "＋ 遷移を追加" };
            btn.style.marginTop = 4;
            mainContainer.Add(btn);
        }

        // ─────────────────────────────────────
        // 右クリックメニュー
        // ─────────────────────────────────────

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Start ノードに設定", _ => _graphView.SetStartNode(AINode));
            evt.menu.AppendSeparator();
            base.BuildContextualMenu(evt);
        }

        // ─────────────────────────────────────
        // 外部API
        // ─────────────────────────────────────

        public Port GetOutputPort(int index) =>
            index >= 0 && index < _outputPorts.Count ? _outputPorts[index] : null;

        public int GetPortIndex(Port port) => _outputPorts.IndexOf(port);

        public void RefreshStartNodeStyle(AINode startNode)
        {
            titleContainer.style.backgroundColor = AINode == startNode ? ColorStart : ColorDefault;
        }
    }
}
