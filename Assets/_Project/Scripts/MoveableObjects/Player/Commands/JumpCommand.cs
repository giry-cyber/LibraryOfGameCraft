namespace LibraryOfGamecraft.Player.Commands
{
    /// <summary>
    /// ジャンプコマンド。JumpHandler にジャンプ要求を登録する。
    /// 実際の Rigidbody 操作は JumpHandler.TryExecuteJump()（FixedUpdate）で行う。
    /// </summary>
    public class JumpCommand : IPlayerCommand
    {
        private readonly JumpHandler _handler;

        public JumpCommand(JumpHandler handler)
        {
            _handler = handler;
        }

        public void Execute() => _handler.RequestJump();
    }
}
