using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // Blackboard の BTKeys.Target へ向かって追跡する。
    // ターゲットが null になったら Failure を返し、Selector が次の子へ移る。
    [CreateAssetMenu(fileName = "MoveToTargetAction", menuName = "LibraryOfGamecraft/BT/Actions/MoveToTarget")]
    public class MoveToTargetAction : BTAction
    {
        private const string KeyNextUpdate = "moveToTarget_nextUpdate";

        [SerializeField] private float _chaseSpeed      = 5f;
        [SerializeField] private float _updateInterval  = 0.3f;

        protected override void OnEnter(BTContext ctx)
        {
            var motor = ctx.Get<CharacterMotor>();
            if (motor != null) motor.SetSpeed(_chaseSpeed);
            ctx.Blackboard.Set(KeyNextUpdate, 0f);
            UpdateDestination(ctx);
        }

        protected override BTStatus OnTick(BTContext ctx)
        {
            var target = ctx.Blackboard.Get<Transform>(BTKeys.Target);
            if (target == null) return BTStatus.Failure;

            var now = Time.time;
            if (now >= ctx.Blackboard.Get<float>(KeyNextUpdate))
            {
                ctx.Blackboard.Set(KeyNextUpdate, now + _updateInterval);
                UpdateDestination(ctx);
            }

            return BTStatus.Running;
        }

        protected override void OnExit(BTContext ctx, BTStatus status)
        {
            var motor = ctx.Get<CharacterMotor>();
            if (motor == null) return;
            motor.ResetSpeed();
            motor.Stop();
        }

        private void UpdateDestination(BTContext ctx)
        {
            var target = ctx.Blackboard.Get<Transform>(BTKeys.Target);
            if (target == null) return;
            var motor = ctx.Get<CharacterMotor>();
            if (motor != null) motor.MoveTo(target.position);
        }
    }
}
