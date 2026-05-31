using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    public abstract class BTComposite : BTNode
    {
        [SerializeField] protected List<BTNode> _children = new();

        public static T Create<T>(params BTNode[] children) where T : BTComposite
        {
            var node = CreateInstance<T>();
            node._children.AddRange(children);
            return node;
        }

#if UNITY_EDITOR
        public IReadOnlyList<BTNode> Children => _children;

        public void Editor_AddChild(BTNode child)
        {
            _children.Add(child);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void Editor_RemoveChild(BTNode child)
        {
            _children.Remove(child);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void Editor_RemoveChildAt(int index)
        {
            _children.RemoveAt(index);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void Editor_SetChildAt(int index, BTNode child)
        {
            _children[index] = child;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
