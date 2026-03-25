using UnityEngine;

namespace LibraryOfGamecraft.Player.States
{
    /// <summary>
    /// 接地・移動状態。WASD で移動しながら、ジャンプや空中落下への遷移を管理する。
    /// </summary>
    public class MoveState : IPlayerState
    {
        public void Enter(PlayerApiHub hub)
        {
            hub.PlayerAnimator.PlayMove(0f);
        }

        public void Update(PlayerApiHub hub)
        {
            // 水平速度を終端速度で正規化してブレンドツリーを更新（Walk ↔ Run）
            float horizontalSpeed = Vector3.ProjectOnPlane(hub.Rigidbody.linearVelocity, Vector3.up).magnitude;
            float normalizedSpeed = horizontalSpeed / hub.TerminalHorizontalVelocity;
            hub.PlayerAnimator.PlayMove(normalizedSpeed);
        }

        public void FixedUpdate(PlayerApiHub hub)
        {
            // PI制御で水平移動力を加える
            hub.MovementMotor.ApplyMovement();

            // 接地中は追加ジャンプを全回復
            if (hub.GroundChecker.IsGrounded)
                hub.JumpHandler.ResetExtraJumps();

            // 足場が消えたら落下へ
            if (!hub.GroundChecker.IsGrounded)
            {
                hub.StateMachine.ChangeState(new FallState());
                return;
            }

            // ジャンプ実行（接地ジャンプ）
            if (hub.JumpHandler.TryExecuteJump())
            {
                hub.StateMachine.ChangeState(new JumpState());
                return;
            }

            // 入力がなくなれば待機状態へ
            if (hub.MovementMotor.CurrentInput.sqrMagnitude < 0.01f)
            {
                hub.StateMachine.ChangeState(new IdleState());
            }
        }

        public void Exit(PlayerApiHub hub) { }
    }
}
