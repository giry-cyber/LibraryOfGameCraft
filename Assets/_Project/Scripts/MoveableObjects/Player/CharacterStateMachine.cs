using System.Collections.Generic;

namespace LibraryOfGamecraft.Player
{
    public class CharacterStateMachine
    {
        private readonly Dictionary<CharacterStateType, ICharacterState> _states
            = new Dictionary<CharacterStateType, ICharacterState>();

        private ICharacterState _currentState;
        private CharacterStateContext _context;

        public ICharacterState CurrentState => _currentState;
        public CharacterStateType CurrentStateType => _currentState?.StateType ?? CharacterStateType.Idle;

        public void Initialize(CharacterStateContext context, CharacterStateType initialState)
        {
            _context = context;

            RegisterState(new IdleState());
            RegisterState(new MoveState());
            RegisterState(new JumpState());
            RegisterState(new FallState());
            RegisterState(new LandingState());
            RegisterState(new AttackState());
            RegisterState(new DodgeState());
            RegisterState(new StunState());

            TransitionTo(initialState);
        }

        public void Tick(float deltaTime)
        {
            _currentState?.Tick(_context, deltaTime);
        }

        /// <summary>現在の State が許可した場合のみ遷移する。</summary>
        public void RequestTransition(CharacterStateType next)
        {
            if (_currentState != null && !_currentState.CanTransitionTo(next)) return;
            TransitionTo(next);
        }

        /// <summary>State の許可に関わらず強制遷移する（Fall・Landing等の物理的強制遷移に使用）。</summary>
        public void ForceTransition(CharacterStateType next)
        {
            TransitionTo(next);
        }

        private void TransitionTo(CharacterStateType next)
        {
            if (!_states.TryGetValue(next, out var nextState)) return;

            _currentState?.Exit(_context);
            _currentState = nextState;
            _currentState.Enter(_context);
        }

        private void RegisterState(ICharacterState state)
        {
            _states[state.StateType] = state;
        }

        public StunState GetStunState() => _states[CharacterStateType.Stun] as StunState;
    }
}
