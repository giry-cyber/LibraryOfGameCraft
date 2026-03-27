namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// InputSnapshot を解釈し、キャラクターの行動意図（Command）を StateMachine へ送る。
    /// 人間入力・AI入力・リプレイ入力はすべて同一の InputSnapshot として渡される。
    /// </summary>
    public class CharacterBrain
    {
        private readonly IPlayerCommand _jumpCommand = new JumpCommand();
        private readonly IPlayerCommand _attackCommand = new AttackCommand();
        private readonly IPlayerCommand _dashCommand = new DashCommand();

        public void Process(InputSnapshot snapshot, CharacterStateContext context)
        {
            if (snapshot.JumpPressed) _jumpCommand.Execute(context);
            if (snapshot.AttackPressed) _attackCommand.Execute(context);
            if (snapshot.DashPressed) _dashCommand.Execute(context);
            // Move は単発ボタンではなく連続入力のため、各 State.Tick() で処理する
        }
    }
}
