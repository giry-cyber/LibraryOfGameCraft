using UnityEngine;
using UnityEngine.AI;

namespace LibraryOfGamecraft.BT
{
    // HomePosition を中心にランダム徘徊する。フォールバック行動として常に Running を返す。
    [CreateAssetMenu(fileName = "WanderAction", menuName = "LibraryOfGamecraft/BT/Actions/Wander")]
    public class WanderAction : BTAction
    {
        private const string KeyHasTarget  = "wander_hasTarget";
        private const string KeyTimeInMove = "wander_timeInMove";

        [SerializeField] private float _wanderRadius = 5f;
        [SerializeField] private float _moveTimeout  = 8f;

        protected override void OnEnter(BTContext ctx)
        {
            ctx.Blackboard.Set(KeyHasTarget, false);
            var motor = ctx.Get<CharacterMotor>();
            if (motor != null) motor.ResetSpeed();
            PickNewTarget(ctx);
        }

        protected override BTStatus OnTick(BTContext ctx)
        {
            var motor = ctx.Get<CharacterMotor>();
            if (motor == null) return BTStatus.Failure;

            if (!ctx.Blackboard.Get<bool>(KeyHasTarget))
            {
                PickNewTarget(ctx);
                return BTStatus.Running;
            }

            if (motor.HasArrived)
            {
                ctx.Blackboard.Set(KeyHasTarget, false);
                return BTStatus.Running;
            }

            if (!motor.IsPathValid)
            {
                ctx.Blackboard.Set(KeyHasTarget, false);
                return BTStatus.Running;
            }

            var elapsed = ctx.Blackboard.Get<float>(KeyTimeInMove) + Time.deltaTime;
            ctx.Blackboard.Set(KeyTimeInMove, elapsed);
            if (elapsed >= _moveTimeout)
                ctx.Blackboard.Set(KeyHasTarget, false);

            return BTStatus.Running;
        }

        protected override void OnExit(BTContext ctx, BTStatus status)
        {
            var motor = ctx.Get<CharacterMotor>();
            if (motor != null) motor.Stop();
            ctx.Blackboard.Set(KeyHasTarget, false);
        }

        private void PickNewTarget(BTContext ctx)
        {
            var home = ctx.Blackboard.Get<Vector3>(BTKeys.HomePosition, ctx.Transform.position);
            var candidate = home + new Vector3(
                Random.Range(-_wanderRadius, _wanderRadius),
                0f,
                Random.Range(-_wanderRadius, _wanderRadius));

            var motor = ctx.Get<CharacterMotor>();
            if (motor == null) return;

            if (NavMesh.SamplePosition(candidate, out var hit, _wanderRadius, NavMesh.AllAreas))
            {
                motor.MoveTo(hit.position);
                ctx.Blackboard.Set(KeyHasTarget, true);
                ctx.Blackboard.Set(KeyTimeInMove, 0f);
            }
        }
    }
}
