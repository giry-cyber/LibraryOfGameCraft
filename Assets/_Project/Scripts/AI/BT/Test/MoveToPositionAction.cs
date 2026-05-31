using UnityEngine;

namespace LibraryOfGamecraft.BT.Test
{
    // 固定座標へ移動して到達したら Success を返すテスト用 Action。
    // BTGraph アセットに登録して Inspector から座標を設定する。
    [CreateAssetMenu(fileName = "MoveToPosition", menuName = "LibraryOfGamecraft/BT/Test/MoveToPosition")]
    public class MoveToPositionAction : BTAction
    {
        [SerializeField] private Vector3 _targetPosition;

        protected override void OnEnter(BTContext ctx)
        {
            Debug.Log($"[MoveToPosition] {ctx.Owner.name}: {_targetPosition} へ移動開始");
            var motor = ctx.Get<CharacterMotor>();
            if (motor != null) motor.MoveTo(_targetPosition);
        }

        protected override BTStatus OnTick(BTContext ctx)
        {
            var motor = ctx.Get<CharacterMotor>();
            if (motor == null) return BTStatus.Failure;
            return motor.HasArrived ? BTStatus.Success : BTStatus.Running;
        }

        protected override void OnExit(BTContext ctx, BTStatus status)
        {
            Debug.Log($"[MoveToPosition] {ctx.Owner.name}: 完了 ({status})");
            if (status != BTStatus.Success)
            {
                var motor = ctx.Get<CharacterMotor>();
                if (motor != null) motor.Stop();
            }
        }
    }
}
