using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "NewAIBehaviourGraph", menuName = "LibraryOfGamecraft/AI/BehaviourGraph")]
    public class AIBehaviourGraph : ScriptableObject
    {
        [SerializeField] private AINode _startNode;
        [SerializeField] private List<AINode> _nodes = new();

        public AINode StartNode => _startNode;
        public IReadOnlyList<AINode> Nodes => _nodes;

        public void SetStartNode(AINode node) => _startNode = node;

        public void AddNode(AINode node)
        {
            if (node != null && !_nodes.Contains(node))
                _nodes.Add(node);
        }

        public void RemoveNode(AINode nodeToRemove)
        {
            _nodes.Remove(nodeToRemove);
            if (_startNode == nodeToRemove)
                _startNode = _nodes.Count > 0 ? _nodes[0] : null;

            // 他ノードの遷移先が削除ノードを参照していたらクリア
            foreach (var n in _nodes)
                for (int i = 0; i < n.Transitions.Count; i++)
                    if (n.Transitions[i].NextNode == nodeToRemove)
                        n.SetTransitionNextNode(i, null);
        }
    }
}
