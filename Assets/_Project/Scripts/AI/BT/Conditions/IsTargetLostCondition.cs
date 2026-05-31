using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // Target が null または指定距離以上なら Success。Chase → 別行動への遷移条件。
    [CreateAssetMenu(fileName = "IsTargetLost", menuName = "LibraryOfGamecraft/BT/Conditions/IsTargetLost")]
    public class IsTargetLostCondition : BTCondition
    {
        [SerializeField] private float _lostRange = 15f;

        protected override bool Check(BTContext ctx)
        {
            var target = ctx.Blackboard.Get<Transform>(BTKeys.Target);
            if (target == null) return true;
            return Vector3.Distance(ctx.Transform.position, target.position) > _lostRange;
        }
    }
}
