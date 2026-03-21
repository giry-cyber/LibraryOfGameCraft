using UnityEngine;
using UnityEngine.InputSystem;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// 入力の読み取りを一元管理するクラス。
    /// 新しい Input System への差し替えはこのクラスだけ変更すればよい。
    /// 他のクラスは InputReader のプロパティを参照するだけでよいため、
    /// 入力実装の変更が全体に波及しない。
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        /// <summary>WASD / 矢印キーの入力（各軸 -1〜1）</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>マウス／右スティックのルック入力（ピクセルデルタ）</summary>
        public Vector2 LookInput { get; private set; }

        /// <summary>このフレームにジャンプキーが押されたか（1フレームのみ true）</summary>
        public bool JumpPressed { get; private set; }

        private InputSystem_Actions _actions;

        private void Awake()
        {
            _actions = new InputSystem_Actions();
            _actions.Player.Jump.performed += _ => JumpPressed = true;
        }

        private void OnEnable()  => _actions.Player.Enable();
        private void OnDisable() => _actions.Player.Disable();

        private void Update()
        {
            MoveInput  = _actions.Player.Move.ReadValue<Vector2>();
            LookInput  = _actions.Player.Look.ReadValue<Vector2>();
        }

        /// <summary>
        /// ジャンプ入力を消費済みにする。
        /// PlayerController が JumpCommand を発行した後に呼び、二重処理を防ぐ。
        /// </summary>
        public void ConsumeJump() => JumpPressed = false;
    }
}
