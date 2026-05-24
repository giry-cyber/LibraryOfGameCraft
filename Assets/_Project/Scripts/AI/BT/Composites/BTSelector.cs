using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // 最初に Success を返した子で止まる（OR）。
    // 全子が Failure → Failure。Running の子がいれば Running を返して次フレームへ。
    [CreateAssetMenu(fileName = "BTSelector", menuName = "LibraryOfGamecraft/BT/Composites/Selector")]
    public class BTSelector : BTComposite
    {
        private string IndexKey => $"__sel_{GetInstanceID()}";

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
                if (status == BTStatus.Success)
                {
                    ctx.Blackboard.Set(IndexKey, 0);
                    return BTStatus.Success;
                }

                i++;
            }

            ctx.Blackboard.Set(IndexKey, 0);
            return BTStatus.Failure;
        }
    }
}
