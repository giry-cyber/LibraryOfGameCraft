namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// 行動不能状態。仕様 23.5 に従い、時間経過・外部延長・即時解除をサポートする。
    /// </summary>
    public class StunState : ICharacterState
    {
        public CharacterStateType StateType => CharacterStateType.Stun;
        public IMovementStrategy MovementStrategy { get; } = new GroundMovementStrategy();

        private float _remainingTime;

        public void Initialize(float stunTime) => _remainingTime = stunTime;
        public void AddStun(float time) => _remainingTime += time;
        public void ForceRecover() => _remainingTime = 0f;

        public void Enter(CharacterStateContext context)
        {
            context.Events.RaiseStateChanged(CharacterStateType.Stun);
        }

        public void Exit(CharacterStateContext context) { }

        public void Tick(CharacterStateContext context, float deltaTime)
        {
            _remainingTime -= deltaTime;
            if (_remainingTime > 0f) return;

            var next = context.CurrentInput.Move.magnitude > 0.1f
                ? CharacterStateType.Move
                : CharacterStateType.Idle;
            context.StateMachine.ForceTransition(next);
        }

        // Stun中は他の通常遷移を受け付けない
        public bool CanTransitionTo(CharacterStateType next) => false;
    }
}
