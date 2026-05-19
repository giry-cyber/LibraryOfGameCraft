using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "TimerCondition", menuName = "LibraryOfGamecraft/AI/Conditions/TimerCondition")]
    public class TimerCondition : AICondition
    {
        [SerializeField] private float _duration = 3f;

        public override bool Evaluate(AIController context)
        {
            return context.ElapsedTimeInState >= _duration;
        }
    }
}
