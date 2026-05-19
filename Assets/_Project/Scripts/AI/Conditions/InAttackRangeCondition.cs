using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "InAttackRangeCondition", menuName = "LibraryOfGamecraft/AI/Conditions/InAttackRangeCondition")]
    public class InAttackRangeCondition : AICondition
    {
        [SerializeField] private float _attackRange = 2f;

        public override bool Evaluate(AIController context)
        {
            if (context.TargetTransform == null) return false;
            return Vector3.Distance(context.SelfTransform.position, context.TargetTransform.position) <= _attackRange;
        }
    }
}
