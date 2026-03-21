using LibraryOfGamecraft.Player.States;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// ステートマシン。現在の状態を保持し、Update / FixedUpdate を現在の状態に委譲する。
    /// 状態の追加・変更はこのクラスを修正せず、IPlayerState の実装を追加するだけでよい。
    /// </summary>
    public class PlayerStateMachine
    {
        public IPlayerState CurrentState { get; private set; }

        private readonly PlayerApiHub _hub;

        public PlayerStateMachine(PlayerApiHub hub, IPlayerState initialState)
        {
            _hub = hub;
            ChangeState(initialState);
        }

        /// <summary>
        /// 現在の状態を終了させ、新しい状態に切り替える。
        /// Exit → Enter の順序を保証する。
        /// </summary>
        public void ChangeState(IPlayerState newState)
        {
            CurrentState?.Exit(_hub);
            CurrentState = newState;
            CurrentState.Enter(_hub);
        }

        public void Update()       => CurrentState?.Update(_hub);
        public void FixedUpdate()  => CurrentState?.FixedUpdate(_hub);
    }
}
