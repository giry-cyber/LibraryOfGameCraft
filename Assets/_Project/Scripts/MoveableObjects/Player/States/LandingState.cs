namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// 着地状態。仕様 23.4 に従い、アニメーション終了ではなく時間条件と入力条件で遷移を決定する。
    /// </summary>
    public class LandingState : ICharacterState
    {
        public CharacterStateType StateType => CharacterStateType.Landing;
        public IMovementStrategy MovementStrategy { get; } = new GroundMovementStrategy();

        private float _elapsed;

        public void Enter(CharacterStateContext context)
        {
            _elapsed = 0f;
            context.Events.RaiseLand();
            context.Events.RaiseStateChanged(CharacterStateType.Landing);
        }

        public void Exit(CharacterStateContext context) { }

        public void Tick(CharacterStateContext context, float deltaTime)
        {
            _elapsed += deltaTime;

            // minLandingTime 未経過または非接地なら遷移しない
            if (_elapsed < context.JumpSettings.MinLandingTime) return;
            if (!context.GroundInfo.IsGrounded) return;

            if (context.CurrentInput.JumpPressed)
                context.StateMachine.RequestTransition(CharacterStateType.Jump);
            else if (context.CurrentInput.Move.magnitude > 0.1f)
                context.StateMachine.RequestTransition(CharacterStateType.Move);
            else
                context.StateMachine.RequestTransition(CharacterStateType.Idle);
        }

        public bool CanTransitionTo(CharacterStateType next)
        {
            return next == CharacterStateType.Jump
                || next == CharacterStateType.Move
                || next == CharacterStateType.Idle
                || next == CharacterStateType.Stun;
        }
    }
}
