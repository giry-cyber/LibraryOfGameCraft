using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    [CreateAssetMenu(fileName = "BTGraph", menuName = "LibraryOfGamecraft/BT/BTGraph")]
    public class BTGraph : ScriptableObject
    {
        [SerializeField] private BTNode       _rootNode;
        [SerializeField, HideInInspector] private List<BTNode> _allNodes = new();

        public BTNode RootNode => _rootNode;

#if UNITY_EDITOR
        public IReadOnlyList<BTNode> AllNodes => _allNodes;

        public void Editor_SetRoot(BTNode root)
        {
            _rootNode = root;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void Editor_AddNode(BTNode node)
        {
            _allNodes.Add(node);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void Editor_RemoveNode(BTNode node)
        {
            _allNodes.Remove(node);
            if (_rootNode == node) _rootNode = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
