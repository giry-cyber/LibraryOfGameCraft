namespace LibraryOfGamecraft.Player.States
{
    /// <summary>
    /// 接地・静止状態。移動入力があれば MoveState、ジャンプで JumpState、
    /// 足場が消えたら FallState へ遷移する。
    /// </summary>
    public class IdleState : IPlayerState
    {
        public void Enter(PlayerApiHub hub)
        {
            // 積分誤差をリセットして以前の状態を引き継がないようにする
            hub.MovementMotor.ResetIntegral();
        }

        public void Update(PlayerApiHub hub) { }

        public void FixedUpdate(PlayerApiHub hub)
        {
            // 接地中は追加ジャンプを全回復
            if (hub.GroundChecker.IsGrounded)
                hub.JumpHandler.ResetExtraJumps();

            // 足場が消えたら落下へ
            if (!hub.GroundChecker.IsGrounded)
            {
                hub.StateMachine.ChangeState(new FallState());
                return;
            }

            // ジャンプ要求があれば実行（接地ジャンプ）
            if (hub.JumpHandler.TryExecuteJump())
            {
                hub.StateMachine.ChangeState(new JumpState());
                return;
            }

            // 移動入力があれば移動状態へ
            if (hub.MovementMotor.CurrentInput.sqrMagnitude > 0.01f)
            {
                hub.StateMachine.ChangeState(new MoveState());
            }
        }

        public void Exit(PlayerApiHub hub) { }
    }
}
