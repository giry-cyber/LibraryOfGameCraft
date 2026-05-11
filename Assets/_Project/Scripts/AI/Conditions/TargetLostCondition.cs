using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "TargetLostCondition", menuName = "LibraryOfGamecraft/AI/Conditions/TargetLostCondition")]
    public class TargetLostCondition : AICondition
    {
        [SerializeField] private float _lostRange = 15f;

        public override bool Evaluate(AIController context)
        {
            if (context.TargetTransform == null) return true;
            return Vector3.Distance(context.SelfTransform.position, context.TargetTransform.position) > _lostRange;
        }
    }
}
