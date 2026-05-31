using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    public abstract class BTDecorator : BTNode
    {
        [SerializeField] protected BTNode _child;

        public override void ForceExit(BTContext ctx)
        {
            if (_child != null) _child.ForceExit(ctx);
        }

        public static T Create<T>(BTNode child) where T : BTDecorator
        {
            var node = CreateInstance<T>();
            node._child = child;
            return node;
        }

#if UNITY_EDITOR
        public BTNode Child => _child;

        public void Editor_SetChild(BTNode child)
        {
            _child = child;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void Editor_ClearChild()
        {
            _child = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
