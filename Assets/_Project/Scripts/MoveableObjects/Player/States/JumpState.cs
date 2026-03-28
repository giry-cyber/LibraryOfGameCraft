namespace LibraryOfGamecraft.Player
{
    public class JumpState : ICharacterState
    {
        public CharacterStateType StateType => CharacterStateType.Jump;
        public IMovementStrategy MovementStrategy { get; } = new AirMovementStrategy();

        public void Enter(CharacterStateContext context)
        {
            context.Motor.VerticalVelocity = context.JumpSettings.JumpForce;
            context.Events.RaiseJump();
            context.Events.RaiseStateChanged(CharacterStateType.Jump);
        }

        public void Exit(CharacterStateContext context) { }

        public void Tick(CharacterStateContext context, float deltaTime)
        {
            // 上昇が終わったらFallへ強制遷移
            if (context.Motor.VerticalVelocity <= 0f)
                context.StateMachine.ForceTransition(CharacterStateType.Fall);
        }

        public bool CanTransitionTo(CharacterStateType next)
        {
            // Jump中はFall・Stunへのみ遷移を受け付ける
            return next == CharacterStateType.Fall || next == CharacterStateType.Stun;
        }
    }
}
