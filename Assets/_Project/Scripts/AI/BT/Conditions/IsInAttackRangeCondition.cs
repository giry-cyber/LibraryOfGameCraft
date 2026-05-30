using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // ターゲットが攻撃射程内にいるなら Success。AttackAction の前に置いて射程を制御する。
    [CreateAssetMenu(fileName = "IsInAttackRange", menuName = "LibraryOfGamecraft/BT/Conditions/IsInAttackRange")]
    public class IsInAttackRangeCondition : BTCondition
    {
        [SerializeField] private float _attackRange = 2f;

        protected override bool Check(BTContext ctx)
        {
            var target = ctx.Blackboard.Get<Transform>(BTKeys.Target);
            if (target == null) return false;
            return Vector3.Distance(ctx.Transform.position, target.position) <= _attackRange;
        }
    }
}
