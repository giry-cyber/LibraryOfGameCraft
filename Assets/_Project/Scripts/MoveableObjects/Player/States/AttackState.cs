namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// 攻撃状態。仕様 23.3 に従い、canJumpCancel が有効な攻撃のみ Jump への遷移を許可する。
    /// </summary>
    public class AttackState : ICharacterState
    {
        public CharacterStateType StateType => CharacterStateType.Attack;
        public IMovementStrategy MovementStrategy { get; } = new GroundMovementStrategy();

        private float _attackDuration = 0.5f;
        private float _elapsed;
        // 攻撃データ拡張時にここを切り替える（仕様 23.3）
        private bool _canJumpCancel = false;

        public void Enter(CharacterStateContext context)
        {
            _elapsed = 0f;
            context.Events.RaiseAttackStart();
            context.Events.RaiseStateChanged(CharacterStateType.Attack);
        }

        public void Exit(CharacterStateContext context) { }

        public void Tick(CharacterStateContext context, float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed < _attackDuration) return;

            var next = context.CurrentInput.Move.magnitude > 0.1f
                ? CharacterStateType.Move
                : CharacterStateType.Idle;
            context.StateMachine.RequestTransition(next);
        }

        public bool CanTransitionTo(CharacterStateType next)
        {
            if (next == CharacterStateType.Jump && _canJumpCancel) return true;
            return next == CharacterStateType.Stun;
        }
    }
}
