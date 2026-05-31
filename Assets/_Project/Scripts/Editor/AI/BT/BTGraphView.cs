using System;
using System.Collections.Generic;
using System.Linq;
using LibraryOfGamecraft.BT;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LibraryOfGamecraft.BT.Editor
{
    public class BTGraphView : GraphView
    {
        private BTGraph _graph;
        private readonly Dictionary<BTNode, BTNodeView> _nodeViews = new();
        private bool _isPopulating;

        public BTGraphView()
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

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                schedule.Execute(RefreshDebugColors).Every(50);
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            });
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                foreach (var view in _nodeViews.Values)
                    view.ResetStatus();
        }

        private void RefreshDebugColors()
        {
            if (!EditorApplication.isPlaying) return;
            int frame = UnityEngine.Time.frameCount;
            foreach (var (node, view) in _nodeViews)
            {
                if (node.EditorLastTickFrame >= frame - 1)
                    view.UpdateStatus(node.EditorLastStatus);
                else
                    view.ResetStatus();
            }
        }

        // ─────────────────────────────────────
        // グラフ生成
        // ─────────────────────────────────────

        public void PopulateGraph(BTGraph graph)
        {
            _isPopulating = true;

            _graph = graph;
            _nodeViews.Clear();
            graphElements.ForEach(RemoveElement);

            if (graph == null)
            {
                _isPopulating = false;
                return;
            }

            foreach (var node in graph.AllNodes)
            {
                if (node == null) continue;
                CreateNodeView(node);
            }

            foreach (var node in graph.AllNodes)
            {
                if (node == null) continue;
                CreateEdgesForNode(node);
            }

            RefreshRootHighlight();

            _isPopulating = false;

            bool allAtOrigin = graph.AllNodes.All(n => n != null && n.EditorPosition == Vector2.zero);
            if (allAtOrigin && graph.AllNodes.Count > 1)
                AutoLayout();

            schedule.Execute(() => FrameAll()).StartingIn(50);
        }

        private BTNodeView CreateNodeView(BTNode node)
        {
            var view = new BTNodeView(node, this);
            AddElement(view);
            _nodeViews[node] = view;
            return view;
        }

        private void CreateEdgesForNode(BTNode node)
        {
            if (!_nodeViews.TryGetValue(node, out var fromView)) return;

            if (node is BTComposite composite)
            {
                for (int i = 0; i < composite.Children.Count; i++)
                {
                    var child = composite.Children[i];
                    if (child == null) continue;
                    if (!_nodeViews.TryGetValue(child, out var toView)) continue;
                    if (i >= fromView.ChildPorts.Count) continue;

                    var edge = fromView.ChildPorts[i].ConnectTo(toView.InputPort);
                    AddElement(edge);
                }
            }
            else if (node is BTDecorator decorator)
            {
                var child = decorator.Child;
                if (child == null) return;
                if (!_nodeViews.TryGetValue(child, out var toView)) return;

                var edge = fromView.OutputPort.ConnectTo(toView.InputPort);
                AddElement(edge);
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
                AppendNodeMenuItems(evt, mousePos);
                evt.menu.AppendSeparator();
            }
            base.BuildContextualMenu(evt);
        }

        private void AppendNodeMenuItems(ContextualMenuPopulateEvent evt, Vector2 mousePos)
        {
            var nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => !t.IsAbstract && typeof(BTNode).IsAssignableFrom(t))
                .OrderBy(t => t.Name);

            foreach (var type in nodeTypes)
            {
                string category = GetCategory(type);
                evt.menu.AppendAction($"ノードを追加/{category}/{type.Name}",
                    _ => AddNode(type, mousePos));
            }
        }

        private static string GetCategory(Type type)
        {
            if (typeof(BTComposite).IsAssignableFrom(type)) return "Composite";
            if (typeof(BTDecorator).IsAssignableFrom(type)) return "Decorator";
            if (typeof(BTCondition).IsAssignableFrom(type)) return "Condition";
            return "Action";
        }

        // ─────────────────────────────────────
        // 変更ハンドラ
        // ─────────────────────────────────────

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isPopulating) return change;

            if (change.edgesToCreate != null)
                foreach (var edge in change.edgesToCreate)
                    SyncEdgeAdded(edge);

            if (change.elementsToRemove != null)
            {
                // ノード削除を先に処理し参照・ポートをクリーンアップする。
                // その後エッジ削除を処理することで、削除済みポートへの誤操作を防ぐ。
                foreach (var elem in change.elementsToRemove)
                    if (elem is BTNodeView nodeView)
                        SyncNodeRemoved(nodeView);

                foreach (var elem in change.elementsToRemove)
                    if (elem is Edge edge)
                        SyncEdgeRemoved(edge);
            }

            return change;
        }

        private void SyncEdgeAdded(Edge edge)
        {
            if (edge.output?.node is not BTNodeView from) return;
            if (edge.input?.node is not BTNodeView to) return;

            if (from.BTNode is BTComposite composite)
            {
                int index = edge.output.userData is int i ? i : 0;
                composite.Editor_SetChildAt(index, to.BTNode);
            }
            else if (from.BTNode is BTDecorator decorator)
            {
                decorator.Editor_SetChild(to.BTNode);
            }
        }

        private void SyncEdgeRemoved(Edge edge)
        {
            if (edge.output?.node is not BTNodeView from) return;

            // ノード削除に伴うエッジ削除は SyncNodeRemoved で処理済みなのでスキップ
            if (!_nodeViews.ContainsKey(from.BTNode)) return;

            if (from.BTNode is BTComposite composite)
            {
                // ポートがまだ有効かを確認（SyncNodeRemoved で除去済みの場合はスキップ）
                if (!from.ChildPorts.Contains(edge.output)) return;
                int index = edge.output.userData is int i ? i : 0;
                if (index < composite.Children.Count)
                    composite.Editor_SetChildAt(index, null);
            }
            else if (from.BTNode is BTDecorator decorator)
            {
                decorator.Editor_ClearChild();
            }
        }

        private void SyncNodeRemoved(BTNodeView nodeView)
        {
            if (_graph == null) return;
            var deleted = nodeView.BTNode;

            // 全ノードを走査して削除ノードへの参照をクリーンアップする
            foreach (var (node, view) in _nodeViews)
            {
                if (node == deleted) continue;

                if (node is BTComposite composite)
                {
                    // 後ろから走査してインデックスずれを防ぐ
                    for (int i = composite.Children.Count - 1; i >= 0; i--)
                    {
                        if (composite.Children[i] == deleted)
                        {
                            composite.Editor_RemoveChildAt(i);
                            view.RemoveChildPortAt(i);
                        }
                    }
                }
                else if (node is BTDecorator decorator && decorator.Child == deleted)
                {
                    decorator.Editor_ClearChild();
                }
            }

            _nodeViews.Remove(deleted);
            _graph.Editor_RemoveNode(deleted);
            AssetDatabase.RemoveObjectFromAsset(deleted);
            AssetDatabase.SaveAssets();
            RefreshRootHighlight();
        }

        // ─────────────────────────────────────
        // ノード追加
        // ─────────────────────────────────────

        private void AddNode(Type type, Vector2 position)
        {
            if (_graph == null) return;

            var node = (BTNode)ScriptableObject.CreateInstance(type);
            node.name = type.Name;
            node.EditorPosition = position;

            Undo.RegisterCreatedObjectUndo(node, $"Add {type.Name}");
            AssetDatabase.AddObjectToAsset(node, _graph);
            _graph.Editor_AddNode(node);
            AssetDatabase.SaveAssets();

            var view = CreateNodeView(node);
            view.SetPosition(new Rect(position, Vector2.zero));
        }

        // ─────────────────────────────────────
        // ルート設定
        // ─────────────────────────────────────

        public void SetAsRoot(BTNodeView targetView)
        {
            if (_graph == null) return;
            _graph.Editor_SetRoot(targetView.BTNode);
            RefreshRootHighlight();
        }

        private void RefreshRootHighlight()
        {
            if (_graph == null) return;
            foreach (var kv in _nodeViews)
                kv.Value.SetAsRoot(kv.Key == _graph.RootNode);
        }

        // ─────────────────────────────────────
        // 自動整列（BFS）
        // ─────────────────────────────────────

        public void AutoLayout()
        {
            if (_graph == null || _nodeViews.Count == 0) return;

            const float nodeW = 220f;
            const float nodeH = 140f;
            const float hGap  = 60f;
            const float vGap  = 30f;

            var visited   = new HashSet<BTNode>();
            var colRows   = new Dictionary<int, int>();
            var positions = new Dictionary<BTNode, Vector2>();

            BTNode start = _graph.RootNode != null && _nodeViews.ContainsKey(_graph.RootNode)
                ? _graph.RootNode
                : _nodeViews.Keys.First();

            var queue = new Queue<(BTNode node, int col)>();
            queue.Enqueue((start, 0));

            while (queue.Count > 0)
            {
                var (node, col) = queue.Dequeue();
                if (visited.Contains(node)) continue;
                visited.Add(node);

                colRows.TryGetValue(col, out int row);
                positions[node] = new Vector2(col * (nodeW + hGap), row * (nodeH + vGap));
                colRows[col] = row + 1;

                if (node is BTComposite c)
                    foreach (var child in c.Children)
                        if (child != null && !visited.Contains(child))
                            queue.Enqueue((child, col + 1));
                else if (node is BTDecorator d && d.Child != null && !visited.Contains(d.Child))
                    queue.Enqueue((d.Child, col + 1));
            }

            int orphanRow = colRows.Values.DefaultIfEmpty(0).Max();
            foreach (var node in _nodeViews.Keys)
            {
                if (!positions.ContainsKey(node))
                    positions[node] = new Vector2(0, (orphanRow++) * (nodeH + vGap));
            }

            foreach (var kv in positions)
            {
                if (!_nodeViews.TryGetValue(kv.Key, out var view)) continue;
                kv.Key.EditorPosition = kv.Value;
                view.SetPosition(new Rect(kv.Value, Vector2.zero));
            }

            schedule.Execute(() => FrameAll()).StartingIn(50);
        }
    }
}
