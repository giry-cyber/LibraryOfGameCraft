using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // 子が完了するたびに繰り返す。
    // _repeatCount = 0 で無限ループ（常に Running を返す）。
    // _repeatCount > 0 で指定回数完了したら Success を返す。
    [CreateAssetMenu(fileName = "BTRepeater", menuName = "LibraryOfGamecraft/BT/Decorators/Repeater")]
    public class BTRepeater : BTDecorator
    {
        [SerializeField] private int _repeatCount = 0;   // 0 = 無限

        private string CountKey => $"__rep_{GetInstanceID()}";

        protected override BTStatus Execute(BTContext ctx)
        {
            if (_child == null) return BTStatus.Failure;

            var status = _child.Tick(ctx);
            if (status == BTStatus.Running) return BTStatus.Running;

            var count = ctx.Blackboard.Get<int>(CountKey, 0) + 1;

            if (_repeatCount > 0 && count >= _repeatCount)
            {
                ctx.Blackboard.Set(CountKey, 0);
                return BTStatus.Success;
            }

            ctx.Blackboard.Set(CountKey, count);
            return BTStatus.Running;
        }
    }
}
