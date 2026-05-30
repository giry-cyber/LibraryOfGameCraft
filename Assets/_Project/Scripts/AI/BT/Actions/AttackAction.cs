using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // ターゲットへ向き直り、AttackCapability.TriggerAttack() を呼んで Success を返す。
    // クールダウンは BTCooldown デコレータで制御する。
    [CreateAssetMenu(fileName = "AttackAction", menuName = "LibraryOfGamecraft/BT/Actions/Attack")]
    public class AttackAction : BTAction
    {
        [SerializeField] private float _rotationSpeed = 360f;

        protected override void OnEnter(BTContext ctx)
        {
            var motor = ctx.Get<CharacterMotor>();
            if (motor != null) motor.Stop();
        }

        protected override BTStatus OnTick(BTContext ctx)
        {
            var target = ctx.Blackboard.Get<Transform>(BTKeys.Target);
            if (target == null) return BTStatus.Failure;

            var motor = ctx.Get<CharacterMotor>();
            if (motor != null) motor.FaceToward(target.position, _rotationSpeed);

            var attack = ctx.Get<AttackCapability>();
            if (attack != null) attack.TriggerAttack();

            return BTStatus.Success;
        }
    }
}
