using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "HasArrivedCondition", menuName = "LibraryOfGamecraft/AI/Conditions/HasArrivedCondition")]
    public class HasArrivedCondition : AICondition
    {
        public override bool Evaluate(AIController context) => context.HasArrived;
    }
}
