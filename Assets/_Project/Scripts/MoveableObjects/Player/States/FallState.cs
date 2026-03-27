namespace LibraryOfGamecraft.Player
{
    public class FallState : ICharacterState
    {
        public CharacterStateType StateType => CharacterStateType.Fall;
        public IMovementStrategy MovementStrategy { get; } = new AirMovementStrategy();

        public void Enter(CharacterStateContext context)
        {
            context.Events.RaiseStateChanged(CharacterStateType.Fall);
        }

        public void Exit(CharacterStateContext context) { }

        public void Tick(CharacterStateContext context, float deltaTime)
        {
            if (context.GroundInfo.IsGrounded && context.Motor.VerticalVelocity <= 0f)
                context.StateMachine.ForceTransition(CharacterStateType.Landing);
        }

        public bool CanTransitionTo(CharacterStateType next)
        {
            return next == CharacterStateType.Landing || next == CharacterStateType.Stun;
        }
    }
}
