using System.Collections.Generic;
using System.Linq;
using LibraryOfGamecraft.Dialogue;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryOfGamecraft.Editor.Dialogue
{
    public class DialogueGraphView : GraphView
    {
        private DialogueSet _currentSet;
        private readonly Dictionary<string, DialogueNodeView> _nodeViews = new();
        private readonly DialogueGraphEditorWindow _window;

        // PopulateGraph中はエッジ生成によるデータ更新を抑制する
        private bool _isPopulating;

        public DialogueGraphView(DialogueGraphEditorWindow window)
        {
            _window = window;

            SetupZoom(0.25f, 2.0f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // グリッド背景
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // ミニマップ
            var minimap = new MiniMap { anchored = true };
            minimap.SetPosition(new Rect(10, 30, 180, 120));
            Add(minimap);

            graphViewChanged = OnGraphViewChanged;

            style.flexGrow = 1;
        }

        // ─────────────────────────────────────
        // グラフ生成
        // ─────────────────────────────────────

        public void PopulateGraph(DialogueSet set)
        {
            _isPopulating = true;

            _currentSet = set;
            _nodeViews.Clear();

            // 既存要素を全削除
            graphElements.ForEach(RemoveElement);

            if (set.Nodes == null)
            {
                _isPopulating = false;
                return;
            }

            // ノードビューを生成
            foreach (var node in set.Nodes)
            {
                if (node == null) continue;
                var view = new DialogueNodeView(node);
                AddElement(view);
                _nodeViews[node.NodeId] = view;
            }

            // エッジを生成
            foreach (var node in set.Nodes)
            {
                if (node == null) continue;
                CreateEdgesForNode(node);
            }

            _isPopulating = false;

            // 位置が未設定（全て原点）ならそのまま表示し、初回は自動整列を提案
            bool needsLayout = set.Nodes.All(n => n != null && n.Position == Vector2.zero);
            if (needsLayout && set.Nodes.Count > 1)
                AutoLayout();

            schedule.Execute(() => FrameAll()).StartingIn(50);
        }

        private void CreateEdgesForNode(DialogueNodeBase node)
        {
            switch (node.NodeType)
            {
                case DialogueNodeType.Message:
                case DialogueNodeType.Event:
                case DialogueNodeType.Sequence:
                    TryCreateEdge(node.NodeId, "output", node.NextNodeId);
                    break;

                case DialogueNodeType.Choice:
                    var cn = (ChoiceNode)node;
                    if (cn.Choices != null)
                        for (int i = 0; i < cn.Choices.Length; i++)
                            TryCreateEdge(node.NodeId, $"choice_{i}", cn.Choices[i].NextNodeId);
                    break;

                case DialogueNodeType.Branch:
                    var bn = (BranchNode)node;
                    if (bn.Branches != null)
                        for (int i = 0; i < bn.Branches.Length; i++)
                            TryCreateEdge(node.NodeId, $"branch_{i}", bn.Branches[i].NextNodeId);
                    TryCreateEdge(node.NodeId, "default", bn.DefaultNextNodeId);
                    break;

                case DialogueNodeType.Jump:
                    // 同セット内のジャンプのみエッジ表示
                    var jn = (JumpNode)node;
                    if (string.IsNullOrEmpty(jn.TargetSetId))
                        TryCreateEdge(node.NodeId, "output", jn.TargetNodeId);
                    break;
            }
        }

        private void TryCreateEdge(string fromId, string portKey, string toId)
        {
            if (string.IsNullOrEmpty(toId)) return;
            if (!_nodeViews.TryGetValue(fromId, out var fromView)) return;
            if (!_nodeViews.TryGetValue(toId, out var toView)) return;

            var outPort = fromView.GetOutputPort(portKey);
            var inPort = toView.InputPort;
            if (outPort == null || inPort == null) return;

            var edge = outPort.ConnectTo(inPort);
            AddElement(edge);
        }

        // ─────────────────────────────────────
        // GraphView オーバーライド
        // ─────────────────────────────────────

        // 接続可能なポートを返す（方向が逆・別ノード）
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) =>
            ports.ToList().Where(p =>
                p.direction != startPort.direction &&
                p.node != startPort.node
            ).ToList();

        // 右クリックメニュー：ノード追加
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView)
            {
                var mousePos = contentViewContainer.WorldToLocal(evt.mousePosition);

                evt.menu.AppendAction("ノードを追加/Message",  _ => AddNode<MessageNode> (DialogueNodeType.Message,  mousePos));
                evt.menu.AppendAction("ノードを追加/Choice",   _ => AddNode<ChoiceNode>  (DialogueNodeType.Choice,   mousePos));
                evt.menu.AppendAction("ノードを追加/Branch",   _ => AddNode<BranchNode>  (DialogueNodeType.Branch,   mousePos));
                evt.menu.AppendAction("ノードを追加/Event",    _ => AddNode<EventNode>   (DialogueNodeType.Event,    mousePos));
                evt.menu.AppendAction("ノードを追加/Sequence", _ => AddNode<SequenceNode>(DialogueNodeType.Sequence, mousePos));
                evt.menu.AppendAction("ノードを追加/Jump",     _ => AddNode<JumpNode>    (DialogueNodeType.Jump,     mousePos));
                evt.menu.AppendAction("ノードを追加/End",      _ => AddNode<EndNode>     (DialogueNodeType.End,      mousePos));
                evt.menu.AppendSeparator();
            }
            base.BuildContextualMenu(evt);
        }

        // ─────────────────────────────────────
        // 変更ハンドラ
        // ─────────────────────────────────────

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isPopulating) return change;

            // エッジ作成：NextNodeId を更新
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    if (edge.output?.node is DialogueNodeView from &&
                        edge.input?.node is DialogueNodeView to)
                    {
                        from.SetNextNodeId(edge.output.name, to.Node.NodeId);
                        MarkDirty();
                    }
                }
            }

            // 要素削除
            if (change.elementsToRemove != null)
            {
                foreach (var elem in change.elementsToRemove)
                {
                    // エッジ削除：NextNodeId をクリア
                    if (elem is Edge edge && edge.output?.node is DialogueNodeView from)
                    {
                        from.SetNextNodeId(edge.output.name, null);
                        MarkDirty();
                    }

                    // ノード削除：DialogueSet.Nodes からも除去
                    if (elem is DialogueNodeView nodeView && _currentSet != null)
                    {
                        _currentSet.Nodes.Remove(nodeView.Node);
                        _nodeViews.Remove(nodeView.Node.NodeId);
                        MarkDirty();
                    }
                }
            }

            // ノード移動：位置を保存
            if (change.movedElements != null)
            {
                foreach (var elem in change.movedElements)
                {
                    if (elem is DialogueNodeView nodeView)
                    {
                        nodeView.SavePosition();
                        MarkDirty();
                    }
                }
            }

            return change;
        }

        // ─────────────────────────────────────
        // ノード追加
        // ─────────────────────────────────────

        private void AddNode<T>(DialogueNodeType nodeType, Vector2 position) where T : DialogueNodeBase, new()
        {
            if (_currentSet == null) return;

            Undo.RecordObject(_currentSet, $"Add {nodeType} Node");

            var node = new T
            {
                NodeType = nodeType,
                NodeId   = GenerateNodeId(),
                Position = position
            };

            _currentSet.Nodes.Add(node);

            var view = new DialogueNodeView(node);
            AddElement(view);
            _nodeViews[node.NodeId] = view;

            MarkDirty();
        }

        private string GenerateNodeId()
        {
            int index = (_currentSet.Nodes?.Count ?? 0) + 1;
            string id = $"Node_{index:000}";
            // 重複回避
            while (_nodeViews.ContainsKey(id))
            {
                index++;
                id = $"Node_{index:000}";
            }
            return id;
        }

        // ─────────────────────────────────────
        // 自動整列（BFS）
        // ─────────────────────────────────────

        public void AutoLayout()
        {
            if (_currentSet == null || _nodeViews.Count == 0) return;

            const float nodeW  = 280f;
            const float nodeH  = 160f;
            const float hGap   = 80f;
            const float vGap   = 40f;

            var visited  = new HashSet<string>();
            var colRows  = new Dictionary<int, int>();   // col → 使用済み行数
            var positions = new Dictionary<string, Vector2>();

            // 開始ノードが存在しなければ最初のノードを起点にする
            string startId = _currentSet.StartNodeId;
            if (!_nodeViews.ContainsKey(startId) && _nodeViews.Count > 0)
                startId = _nodeViews.Keys.First();

            var queue = new Queue<(string nodeId, int col)>();
            queue.Enqueue((startId, 0));

            while (queue.Count > 0)
            {
                var (nodeId, col) = queue.Dequeue();
                if (visited.Contains(nodeId)) continue;
                visited.Add(nodeId);

                colRows.TryGetValue(col, out int row);
                positions[nodeId] = new Vector2(col * (nodeW + hGap), row * (nodeH + vGap));
                colRows[col] = row + 1;

                if (!_nodeViews.TryGetValue(nodeId, out var view)) continue;
                foreach (var nextId in view.GetAllNextNodeIds())
                    if (!visited.Contains(nextId))
                        queue.Enqueue((nextId, col + 1));
            }

            // 未到達ノード（孤立）を末尾に配置
            int orphanRow = colRows.Values.DefaultIfEmpty(0).Max();
            foreach (var kv in _nodeViews)
            {
                if (!positions.ContainsKey(kv.Key))
                {
                    positions[kv.Key] = new Vector2(0, (orphanRow++) * (nodeH + vGap));
                }
            }

            // 位置を反映してデータに保存
            foreach (var kv in positions)
            {
                if (!_nodeViews.TryGetValue(kv.Key, out var view)) continue;
                view.Node.Position = kv.Value;
                view.SetPosition(new Rect(kv.Value, Vector2.zero));
            }

            MarkDirty();
            schedule.Execute(() => FrameAll()).StartingIn(50);
        }

        // ─────────────────────────────────────
        // ユーティリティ
        // ─────────────────────────────────────

        private void MarkDirty()
        {
            if (_currentSet != null)
                EditorUtility.SetDirty(_currentSet);
        }
    }
}
