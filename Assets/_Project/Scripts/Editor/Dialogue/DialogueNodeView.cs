using System.Collections.Generic;
using LibraryOfGamecraft.Dialogue;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryOfGamecraft.Editor.Dialogue
{
    public class DialogueNodeView : Node
    {
        public DialogueNodeBase Node { get; private set; }
        public Port InputPort { get; private set; }

        private readonly Dictionary<string, Port> _outputPorts = new Dictionary<string, Port>();

        private static readonly Dictionary<DialogueNodeType, Color> NodeColors = new()
        {
            { DialogueNodeType.Message,  new Color(0.22f, 0.42f, 0.22f) },
            { DialogueNodeType.Choice,   new Color(0.20f, 0.30f, 0.50f) },
            { DialogueNodeType.Branch,   new Color(0.48f, 0.40f, 0.10f) },
            { DialogueNodeType.Event,    new Color(0.48f, 0.20f, 0.20f) },
            { DialogueNodeType.Sequence, new Color(0.38f, 0.22f, 0.48f) },
            { DialogueNodeType.Jump,     new Color(0.18f, 0.42f, 0.40f) },
            { DialogueNodeType.End,      new Color(0.28f, 0.28f, 0.28f) },
        };

        public DialogueNodeView(DialogueNodeBase node)
        {
            Node = node;
            title = $"[{node.NodeType}]  {node.NodeId}";

            SetPosition(new Rect(node.Position, Vector2.zero));
            ApplyTypeStyle();
            CreateInputPort();
            CreateOutputPorts();
            CreateContentArea();

            RefreshExpandedState();
            RefreshPorts();
        }

        // ─────────────────────────────────────
        // 構築
        // ─────────────────────────────────────

        private void ApplyTypeStyle()
        {
            if (NodeColors.TryGetValue(Node.NodeType, out var color))
                titleContainer.style.backgroundColor = color;
        }

        private void CreateInputPort()
        {
            InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);
        }

        private void CreateOutputPorts()
        {
            switch (Node.NodeType)
            {
                case DialogueNodeType.Message:
                case DialogueNodeType.Event:
                case DialogueNodeType.Sequence:
                    AddOutputPort("output", "Next");
                    break;

                case DialogueNodeType.Choice:
                    var choiceNode = (ChoiceNode)Node;
                    if (choiceNode.Choices != null)
                        for (int i = 0; i < choiceNode.Choices.Length; i++)
                        {
                            var label = !string.IsNullOrEmpty(choiceNode.Choices[i].ChoiceText)
                                ? choiceNode.Choices[i].ChoiceText
                                : $"選択肢 {i + 1}";
                            AddOutputPort($"choice_{i}", label);
                        }
                    break;

                case DialogueNodeType.Branch:
                    var branchNode = (BranchNode)Node;
                    if (branchNode.Branches != null)
                        for (int i = 0; i < branchNode.Branches.Length; i++)
                            AddOutputPort($"branch_{i}", $"分岐 {i + 1}");
                    AddOutputPort("default", "Default");
                    break;

                case DialogueNodeType.Jump:
                    AddOutputPort("output", $"→ {((JumpNode)Node).TargetSetId}");
                    break;

                case DialogueNodeType.End:
                    // 出力ポートなし
                    break;
            }
        }

        private void CreateContentArea()
        {
            var container = new VisualElement
            {
                style = { paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 4 }
            };

            switch (Node.NodeType)
            {
                case DialogueNodeType.Message:
                    var msg = (MessageNode)Node;
                    if (!string.IsNullOrEmpty(msg.SpeakerDisplayName))
                        container.Add(MakeBoldLabel(msg.SpeakerDisplayName));
                    container.Add(MakeBodyLabel(Truncate(msg.Text, 60)));
                    break;

                case DialogueNodeType.Choice:
                    var choice = (ChoiceNode)Node;
                    if (!string.IsNullOrEmpty(choice.PromptText))
                        container.Add(MakeBodyLabel(Truncate(choice.PromptText, 40)));
                    break;

                case DialogueNodeType.Branch:
                    container.Add(MakeBodyLabel("条件分岐"));
                    break;

                case DialogueNodeType.Event:
                    var ev = (EventNode)Node;
                    if (ev.Events != null)
                        foreach (var e in ev.Events)
                            container.Add(MakeBodyLabel($"► {e.EventId}"));
                    break;

                case DialogueNodeType.Jump:
                    var jump = (JumpNode)Node;
                    container.Add(MakeBodyLabel($"Set: {jump.TargetSetId}"));
                    container.Add(MakeBodyLabel($"Node: {jump.TargetNodeId}"));
                    break;

                case DialogueNodeType.End:
                    if (!string.IsNullOrEmpty(((EndNode)Node).EndReason))
                        container.Add(MakeBodyLabel(((EndNode)Node).EndReason));
                    break;
            }

            mainContainer.Add(container);
        }

        // ─────────────────────────────────────
        // 外部API
        // ─────────────────────────────────────

        public Port GetOutputPort(string portKey) =>
            _outputPorts.TryGetValue(portKey, out var p) ? p : null;

        public void SavePosition()
        {
            Node.Position = GetPosition().position;
        }

        // エッジ接続時に対応するNextNodeIdを更新する
        public void SetNextNodeId(string portKey, string targetNodeId)
        {
            switch (Node.NodeType)
            {
                case DialogueNodeType.Message:
                case DialogueNodeType.Event:
                case DialogueNodeType.Sequence:
                    Node.NextNodeId = targetNodeId;
                    break;

                case DialogueNodeType.Choice:
                    if (portKey.StartsWith("choice_") && int.TryParse(portKey[7..], out int ci))
                    {
                        var cn = (ChoiceNode)Node;
                        if (cn.Choices != null && ci < cn.Choices.Length)
                            cn.Choices[ci].NextNodeId = targetNodeId;
                    }
                    break;

                case DialogueNodeType.Branch:
                    if (portKey.StartsWith("branch_") && int.TryParse(portKey[7..], out int bi))
                    {
                        var bn = (BranchNode)Node;
                        if (bn.Branches != null && bi < bn.Branches.Length)
                            bn.Branches[bi].NextNodeId = targetNodeId;
                    }
                    else if (portKey == "default")
                    {
                        ((BranchNode)Node).DefaultNextNodeId = targetNodeId;
                    }
                    break;

                case DialogueNodeType.Jump:
                    ((JumpNode)Node).TargetNodeId = targetNodeId;
                    break;
            }
        }

        // 全遷移先NodeIdを列挙（AutoLayout用）
        public IEnumerable<string> GetAllNextNodeIds()
        {
            switch (Node.NodeType)
            {
                case DialogueNodeType.Message:
                case DialogueNodeType.Event:
                case DialogueNodeType.Sequence:
                    if (!string.IsNullOrEmpty(Node.NextNodeId)) yield return Node.NextNodeId;
                    break;

                case DialogueNodeType.Choice:
                    var cn = (ChoiceNode)Node;
                    if (cn.Choices != null)
                        foreach (var c in cn.Choices)
                            if (!string.IsNullOrEmpty(c.NextNodeId)) yield return c.NextNodeId;
                    break;

                case DialogueNodeType.Branch:
                    var bn = (BranchNode)Node;
                    if (bn.Branches != null)
                        foreach (var b in bn.Branches)
                            if (!string.IsNullOrEmpty(b.NextNodeId)) yield return b.NextNodeId;
                    if (!string.IsNullOrEmpty(bn.DefaultNextNodeId)) yield return bn.DefaultNextNodeId;
                    break;

                case DialogueNodeType.Jump:
                    var jn = (JumpNode)Node;
                    if (!string.IsNullOrEmpty(jn.TargetNodeId)) yield return jn.TargetNodeId;
                    break;
            }
        }

        // ─────────────────────────────────────
        // ユーティリティ
        // ─────────────────────────────────────

        private Port AddOutputPort(string portKey, string label)
        {
            var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = label;
            port.name = portKey;
            outputContainer.Add(port);
            _outputPorts[portKey] = port;
            return port;
        }

        private static Label MakeBoldLabel(string text) => new Label(text)
        {
            style = { unityFontStyleAndWeight = FontStyle.Bold, whiteSpace = WhiteSpace.Normal }
        };

        private static Label MakeBodyLabel(string text) => new Label(text)
        {
            style = { fontSize = 11, whiteSpace = WhiteSpace.Normal, color = new Color(0.8f, 0.8f, 0.8f) }
        };

        private static string Truncate(string text, int max) =>
            string.IsNullOrEmpty(text) ? string.Empty
            : text.Length <= max ? text
            : text[..max] + "…";
    }
}
