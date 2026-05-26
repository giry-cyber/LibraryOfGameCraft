using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // 子を1つ持ち、その結果を加工して返すノードの基底。
    public abstract class BTDecorator : BTNode
    {
        [SerializeField] protected BTNode _child;

        // コードからテストツリーを組み立てるファクトリ
        public static T Create<T>(BTNode child) where T : BTDecorator
        {
            var node = CreateInstance<T>();
            node._child = child;
            return node;
        }
    }
}
