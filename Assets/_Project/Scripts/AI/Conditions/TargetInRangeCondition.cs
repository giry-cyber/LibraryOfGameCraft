using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "TargetInRangeCondition", menuName = "LibraryOfGamecraft/AI/Conditions/TargetInRangeCondition")]
    public class TargetInRangeCondition : AICondition
    {
        [SerializeField] private float _range = 12f;

        public override bool Evaluate(AIController context)
        {
            if (context.TargetTransform == null) return false;
            return Vector3.Distance(context.SelfTransform.position, context.TargetTransform.position) <= _range;
        }
    }
}
