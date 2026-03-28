using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// 各 State が参照するすべての共有依存を束ねたコンテキスト。
    /// State は入力デバイスや具体クラスに直接依存しない。
    /// </summary>
    public class CharacterStateContext
    {
        public ICharacterMotor Motor;
        public CharacterStateMachine StateMachine;
        public PlayerEventSystem Events;
        public MovementTuning MovementTuning;
        public JumpSettings JumpSettings;
        public GravitySettings GravitySettings;
        public InputSnapshot CurrentInput;
        public Transform CameraTransform;

        public GroundInfo GroundInfo => Motor.GroundInfo;
    }
}
