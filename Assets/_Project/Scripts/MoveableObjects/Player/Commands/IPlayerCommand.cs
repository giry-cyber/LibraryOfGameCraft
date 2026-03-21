namespace LibraryOfGamecraft.Player.Commands
{
    /// <summary>
    /// コマンドパターンの基底インターフェース。
    /// MoveCommand / JumpCommand など全コマンドはこれを実装する。
    /// 将来の DashCommand / AttackCommand もこれを実装して追加する。
    /// </summary>
    public interface IPlayerCommand
    {
        void Execute();
    }
}
