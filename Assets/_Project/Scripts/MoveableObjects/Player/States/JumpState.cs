namespace LibraryOfGamecraft.Player.States
{
    /// <summary>
    /// ジャンプ上昇中の状態。速度 Y が 0 以下になったら FallState へ遷移する。
    /// 空中でもう一度ジャンプが成功した場合は JumpState を再入して上昇を再開する。
    /// </summary>
    public class JumpState : IPlayerState
    {
        public void Enter(PlayerApiHub hub) { }

        public void Update(PlayerApiHub hub) { }

        public void FixedUpdate(PlayerApiHub hub)
        {
            // 空中でも左右の移動は維持する（空中制御）
            hub.MovementMotor.ApplyMovement();

            // 2段ジャンプ：要求があれば JumpState を再入して上昇をリセット
            if (hub.JumpHandler.TryExecuteJump())
            {
                hub.StateMachine.ChangeState(new JumpState());
                return;
            }

            // 頂点を過ぎたら落下へ
            if (hub.Rigidbody.linearVelocity.y <= 0f)
            {
                hub.StateMachine.ChangeState(new FallState());
            }
        }

        public void Exit(PlayerApiHub hub) { }
    }
}
