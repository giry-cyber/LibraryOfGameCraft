using System;
using System.Collections.Generic;
using System.Linq;
using LibraryOfGamecraft.AI;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryOfGamecraft.Editor.AI
{
    public class AIGraphView : GraphView
    {
        private AIBehaviourGraph _graph;
        private readonly Dictionary<AINode, AINodeView> _nodeViews = new();

        // PopulateGraph中はエッジ生成によるデータ更新を抑制する
        private bool _isPopulating;

        public AIGraphView(AIGraphEditorWindow window)
        {
            SetupZoom(0.25f, 2.0f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            var minimap = new MiniMap { anchored = true };
            minimap.SetPosition(new Rect(10, 30, 180, 120));
            Add(minimap);

            graphViewChanged = OnGraphViewChanged;
            style.flexGrow = 1;
        }

        // ─────────────────────────────────────
        // グラフ生成
        // ─────────────────────────────────────

        public void PopulateGraph(AIBehaviourGraph graph)
        {
            _isPopulating = true;
            _graph = graph;
            _nodeViews.Clear();
            graphElements.ForEach(RemoveElement);

            // StartNode から到達可能なノードを _nodes に自動登録（手動アセット設定との互換）
            if (graph.Nodes.Count == 0 && graph.StartNode != null)
                DiscoverNodes(graph);

            foreach (var node in graph.Nodes)
            {
                if (node == null) continue;
                _nodeViews[node] = CreateNodeView(node);
            }

            foreach (var node in graph.Nodes)
            {
                if (node == null) continue;
                CreateEdgesForNode(node);
            }

            _isPopulating = false;

            bool needsLayout = graph.Nodes.All(n => n != null && n.Position == Vector2.zero);
            if (needsLayout && graph.Nodes.Count > 1)
                AutoLayout();

            schedule.Execute(() => FrameAll()).StartingIn(50);
        }

        // StartNode から Transition を BFS で辿り、到達可能な全ノードを graph._nodes に登録する
        private static void DiscoverNodes(AIBehaviourGraph graph)
        {
            var visited = new HashSet<AINode>();
            var queue   = new Queue<AINode>();
            queue.Enqueue(graph.StartNode);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node == null || visited.Contains(node)) continue;
                visited.Add(node);
                graph.AddNode(node);

                foreach (var t in node.Transitions)
                    if (t.NextNode != null)
                        queue.Enqueue(t.NextNode);
            }

            EditorUtility.SetDirty(graph);
        }

        private AINodeView CreateNodeView(AINode node)
        {
            var view = new AINodeView(node, _graph, this);
            AddElement(view);
            return view;
        }

        private void CreateEdgesForNode(AINode node)
        {
            if (!_nodeViews.TryGetValue(node, out var fromView)) return;
            for (int i = 0; i < node.Transitions.Count; i++)
            {
                var nextNode = node.Transitions[i].NextNode;
                if (nextNode == null) continue;
                if (!_nodeViews.TryGetValue(nextNode, out var toView)) continue;

                var outPort = fromView.GetOutputPort(i);
                if (outPort == null || toView.InputPort == null) continue;

                AddElement(outPort.ConnectTo(toView.InputPort));
            }
        }

        // ─────────────────────────────────────
        // GraphView オーバーライド
        // ─────────────────────────────────────

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) =>
            ports.ToList().Where(p =>
                p.direction != startPort.direction &&
                p.node != startPort.node
            ).ToList();

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView)
            {
                var mousePos = contentViewContainer.WorldToLocal(evt.mousePosition);
                foreach (var type in GetAINodeTypes())
                {
                    var captured = type;
                    evt.menu.AppendAction($"ノードを追加/{captured.Name}", _ => AddNode(captured, mousePos));
                }
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

            // エッジ作成：遷移先を更新
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    if (edge.output?.node is AINodeView from && edge.input?.node is AINodeView to)
                    {
                        int idx = from.GetPortIndex(edge.output);
                        if (idx >= 0)
                        {
                            from.AINode.SetTransitionNextNode(idx, to.AINode);
                            MarkDirty(from.AINode);
                        }
                    }
                }
            }

            // 要素削除
            if (change.elementsToRemove != null)
            {
                foreach (var elem in change.elementsToRemove)
                {
                    // エッジ削除：遷移先をクリア
                    if (elem is Edge edge && edge.output?.node is AINodeView from)
                    {
                        int idx = from.GetPortIndex(edge.output);
                        if (idx >= 0)
                        {
                            from.AINode.SetTransitionNextNode(idx, null);
                            MarkDirty(from.AINode);
                        }
                    }

                    // ノード削除：グラフとサブアセットから除去
                    if (elem is AINodeView nodeView)
                    {
                        _graph.RemoveNode(nodeView.AINode);
                        _nodeViews.Remove(nodeView.AINode);
                        AssetDatabase.RemoveObjectFromAsset(nodeView.AINode);
                        MarkDirty(_graph);
                    }
                }
            }

            // ノード移動：座標を保存
            if (change.movedElements != null)
            {
                foreach (var elem in change.movedElements)
                {
                    if (elem is AINodeView nodeView)
                    {
                        nodeView.AINode.Position = nodeView.GetPosition().position;
                        MarkDirty(nodeView.AINode);
                    }
                }
            }

            return change;
        }

        // ─────────────────────────────────────
        // ノード追加
        // ─────────────────────────────────────

        private void AddNode(Type nodeType, Vector2 position)
        {
            if (_graph == null) return;

            Undo.RecordObject(_graph, $"Add {nodeType.Name}");

            var node = (AINode)ScriptableObject.CreateInstance(nodeType);
            node.name = nodeType.Name;
            node.Position = position;

            AssetDatabase.AddObjectToAsset(node, _graph);
            _graph.AddNode(node);

            _nodeViews[node] = CreateNodeView(node);

            MarkDirty(node);
            MarkDirty(_graph);
        }

        // ─────────────────────────────────────
        // 外部API（AINodeView から呼ぶ）
        // ─────────────────────────────────────

        public void SetStartNode(AINode node)
        {
            Undo.RecordObject(_graph, "Set Start Node");
            _graph.SetStartNode(node);
            foreach (var kv in _nodeViews)
                kv.Value.RefreshStartNodeStyle(_graph.StartNode);
            MarkDirty(_graph);
        }

        // ─────────────────────────────────────
        // 自動整列（BFS）
        // ─────────────────────────────────────

        public void AutoLayout()
        {
            if (_graph == null || _nodeViews.Count == 0) return;

            const float nodeW = 240f;
            const float nodeH = 150f;
            const float hGap  = 80f;
            const float vGap  = 40f;

            var visited   = new HashSet<AINode>();
            var colRows   = new Dictionary<int, int>();
            var positions = new Dictionary<AINode, Vector2>();

            var startNode = _graph.StartNode ?? _graph.Nodes.FirstOrDefault();
            if (startNode == null) return;

            var queue = new Queue<(AINode node, int col)>();
            queue.Enqueue((startNode, 0));

            while (queue.Count > 0)
            {
                var (node, col) = queue.Dequeue();
                if (visited.Contains(node)) continue;
                visited.Add(node);

                colRows.TryGetValue(col, out int row);
                positions[node] = new Vector2(col * (nodeW + hGap), row * (nodeH + vGap));
                colRows[col] = row + 1;

                foreach (var t in node.Transitions)
                    if (t.NextNode != null && !visited.Contains(t.NextNode))
                        queue.Enqueue((t.NextNode, col + 1));
            }

            // 孤立ノードを末尾に配置
            int orphanRow = colRows.Values.DefaultIfEmpty(0).Max();
            foreach (var node in _graph.Nodes)
            {
                if (!positions.ContainsKey(node))
                    positions[node] = new Vector2(0, (orphanRow++) * (nodeH + vGap));
            }

            foreach (var kv in positions)
            {
                if (!_nodeViews.TryGetValue(kv.Key, out var view)) continue;
                kv.Key.Position = kv.Value;
                view.SetPosition(new Rect(kv.Value, Vector2.zero));
                MarkDirty(kv.Key);
            }

            schedule.Execute(() => FrameAll()).StartingIn(50);
        }

        // ─────────────────────────────────────
        // ユーティリティ
        // ─────────────────────────────────────

        private void MarkDirty(UnityEngine.Object obj) => EditorUtility.SetDirty(obj);

        private static IEnumerable<Type> GetAINodeTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => t.IsSubclassOf(typeof(AINode)) && !t.IsAbstract);
        }
    }
}
