using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    public abstract class BTComposite : BTNode
    {
        [SerializeField] protected List<BTNode> _children = new();

        // コードからテストツリーを組み立てるファクトリ
        public static T Create<T>(params BTNode[] children) where T : BTComposite
        {
            var node = CreateInstance<T>();
            node._children.AddRange(children);
            return node;
        }
    }
}
