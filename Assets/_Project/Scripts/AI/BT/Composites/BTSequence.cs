using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // 全子が Success を返したら Success。
    // 一つでも Failure → 即 Failure。Running の子がいれば Running を返して次フレームへ。
    [CreateAssetMenu(fileName = "BTSequence", menuName = "LibraryOfGamecraft/BT/Composites/Sequence")]
    public class BTSequence : BTComposite
    {
        // Running 中の子インデックスを Blackboard に保存することで、
        // ScriptableObject を複数の BTRunner で共有してもランタイム状態が混在しない。
        private string IndexKey => $"__seq_{GetInstanceID()}";

        public override BTStatus Tick(BTContext ctx)
        {
            var i = ctx.Blackboard.Get<int>(IndexKey, 0);

            while (i < _children.Count)
            {
                var status = _children[i].Tick(ctx);

                if (status == BTStatus.Running)
                {
                    ctx.Blackboard.Set(IndexKey, i);
                    return BTStatus.Running;
                }
                if (status == BTStatus.Failure)
                {
                    ctx.Blackboard.Set(IndexKey, 0);
                    return BTStatus.Failure;
                }

                i++;
            }

            ctx.Blackboard.Set(IndexKey, 0);
            return BTStatus.Success;
        }
    }
}
