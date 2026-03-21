using UnityEngine;

namespace LibraryOfGamecraft.Player.Commands
{
    /// <summary>
    /// 移動コマンド。Update で生成され、MovementMotor に入力値を渡す。
    /// 物理の適用は FixedUpdate に委ねる（Update/FixedUpdate の分離を守る）。
    /// </summary>
    public class MoveCommand : IPlayerCommand
    {
        private readonly MovementMotor _motor;
        private readonly Vector2 _input;

        public MoveCommand(MovementMotor motor, Vector2 input)
        {
            _motor = motor;
            _input = input;
        }

        public void Execute() => _motor.SetMoveInput(_input);
    }
}
