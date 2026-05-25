using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // Blackboard に Target が存在すれば Success。Wander → Chase の遷移条件。
    [CreateAssetMenu(fileName = "IsTargetDetected", menuName = "LibraryOfGamecraft/BT/Conditions/IsTargetDetected")]
    public class IsTargetDetectedCondition : BTCondition
    {
        protected override bool Check(BTContext ctx)
        {
            var target = ctx.Blackboard.Get<Transform>(BTKeys.Target);
            return target != null;
        }
    }
}
