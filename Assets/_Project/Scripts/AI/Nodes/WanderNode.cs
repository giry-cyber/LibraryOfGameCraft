using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "WanderNode", menuName = "LibraryOfGamecraft/AI/Nodes/WanderNode")]
    public class WanderNode : AINode
    {
        private const string KeyHasTarget = "wander_hasTarget";
        private const string KeyTarget = "wander_target";

        [SerializeField] private float _wanderRadius = 5f;
        [SerializeField] private float _arrivalDistance = 0.5f;

        public override void OnEnter(AIController context)
        {
            context.Blackboard.Set(KeyHasTarget, false);
            PickNewTarget(context);
        }

        public override void Tick(AIController context)
        {
            if (!context.Blackboard.Get<bool>(KeyHasTarget))
            {
                PickNewTarget(context);
                return;
            }

            var target = context.Blackboard.Get<Vector3>(KeyTarget);
            var toTarget = target - context.SelfTransform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= _arrivalDistance)
            {
                context.Blackboard.Set(KeyHasTarget, false);
                context.DesiredMoveDirection = Vector3.zero;
            }
            else
            {
                context.DesiredMoveDirection = toTarget.normalized;
            }
        }

        public override void OnExit(AIController context)
        {
            context.DesiredMoveDirection = Vector3.zero;
            context.Blackboard.Set(KeyHasTarget, false);
        }

        private void PickNewTarget(AIController context)
        {
            var randomOffset = new Vector3(
                Random.Range(-_wanderRadius, _wanderRadius),
                0f,
                Random.Range(-_wanderRadius, _wanderRadius)
            );
            context.Blackboard.Set(KeyTarget, context.HomePosition + randomOffset);
            context.Blackboard.Set(KeyHasTarget, true);
        }
    }
}
