namespace LibraryOfGamecraft.Player
{
    public class MoveState : ICharacterState
    {
        public CharacterStateType StateType => CharacterStateType.Move;
        public IMovementStrategy MovementStrategy { get; } = new GroundMovementStrategy();

        public void Enter(CharacterStateContext context)
        {
            context.Events.RaiseStateChanged(CharacterStateType.Move);
        }

        public void Exit(CharacterStateContext context) { }

        public void Tick(CharacterStateContext context, float deltaTime)
        {
            if (!context.GroundInfo.IsGrounded)
            {
                context.StateMachine.ForceTransition(CharacterStateType.Fall);
                return;
            }

            if (context.Motor.HorizontalVelocity.magnitude < 0.05f)
                context.StateMachine.RequestTransition(CharacterStateType.Idle);
        }

        public bool CanTransitionTo(CharacterStateType next) => true;
    }
}
