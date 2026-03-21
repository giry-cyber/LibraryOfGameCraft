namespace LibraryOfGamecraft.Player.States
{
    /// <summary>
    /// 空中落下状態。接地で地上ステートへ、追加ジャンプで JumpState へ遷移する。
    /// 足場から歩き出して落ちた場合もここに入る。
    /// </summary>
    public class FallState : IPlayerState
    {
        public void Enter(PlayerApiHub hub) { }

        public void Update(PlayerApiHub hub) { }

        public void FixedUpdate(PlayerApiHub hub)
        {
            // 落下中も空中制御は可能
            hub.MovementMotor.ApplyMovement();

            // 追加ジャンプ（2段ジャンプ残数があれば実行）
            if (hub.JumpHandler.TryExecuteJump())
            {
                hub.StateMachine.ChangeState(new JumpState());
                return;
            }

            // 着地したら地上ステートへ戻る
            if (hub.GroundChecker.IsGrounded)
            {
                hub.JumpHandler.ResetExtraJumps();

                if (hub.MovementMotor.CurrentInput.sqrMagnitude > 0.01f)
                    hub.StateMachine.ChangeState(new MoveState());
                else
                    hub.StateMachine.ChangeState(new IdleState());
            }
        }

        public void Exit(PlayerApiHub hub) { }
    }
}
