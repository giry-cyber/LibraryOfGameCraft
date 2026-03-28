namespace LibraryOfGamecraft.Player
{
    public class DodgeState : ICharacterState
    {
        public CharacterStateType StateType => CharacterStateType.Dodge;
        public IMovementStrategy MovementStrategy { get; } = new GroundMovementStrategy();

        private float _dodgeDuration = 0.4f;
        private float _elapsed;

        public void Enter(CharacterStateContext context)
        {
            _elapsed = 0f;
            context.Events.RaiseStateChanged(CharacterStateType.Dodge);
        }

        public void Exit(CharacterStateContext context) { }

        public void Tick(CharacterStateContext context, float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed < _dodgeDuration) return;

            var next = context.CurrentInput.Move.magnitude > 0.1f
                ? CharacterStateType.Move
                : CharacterStateType.Idle;
            context.StateMachine.RequestTransition(next);
        }

        public bool CanTransitionTo(CharacterStateType next) => next == CharacterStateType.Stun;
    }
}
