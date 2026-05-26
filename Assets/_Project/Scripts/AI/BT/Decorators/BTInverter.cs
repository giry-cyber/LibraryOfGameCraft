using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // 子の Success と Failure を反転する。Running はそのまま通す。
    // 例: Inverter(IsTargetDetected) → ターゲットがいないときだけ Success
    [CreateAssetMenu(fileName = "BTInverter", menuName = "LibraryOfGamecraft/BT/Decorators/Inverter")]
    public class BTInverter : BTDecorator
    {
        protected override BTStatus Execute(BTContext ctx)
        {
            if (_child == null) return BTStatus.Failure;

            return _child.Tick(ctx) switch
            {
                BTStatus.Success => BTStatus.Failure,
                BTStatus.Failure => BTStatus.Success,
                _                => BTStatus.Running,
            };
        }
    }
}
