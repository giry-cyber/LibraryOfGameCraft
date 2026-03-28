using UnityEngine;
using UnityEngine.InputSystem;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// Unity Input System からフレーム単位の InputSnapshot を生成する。
    /// 人間・AI・リプレイ入力はすべてこの形式に変換してから Brain に渡す。
    /// </summary>
    public class PlayerInputReader
    {
        private bool _prevJump;
        private bool _prevAttack;
        private bool _prevDash;
        private bool _prevInteract;

        public InputSnapshot ReadSnapshot()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            var move = Vector2.zero;
            var look = Vector2.zero;
            bool jumpHeld = false, attackHeld = false, dashHeld = false, interactHeld = false;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;

                jumpHeld |= keyboard.spaceKey.isPressed;
                attackHeld |= keyboard.jKey.isPressed;
                dashHeld |= keyboard.leftShiftKey.isPressed;
                interactHeld |= keyboard.eKey.isPressed;
            }

            if (gamepad != null)
            {
                move += gamepad.leftStick.ReadValue();
                look += gamepad.rightStick.ReadValue();
                jumpHeld |= gamepad.buttonSouth.isPressed;
                attackHeld |= gamepad.buttonWest.isPressed;
                dashHeld |= gamepad.buttonEast.isPressed;
                interactHeld |= gamepad.buttonNorth.isPressed;
            }

            var snapshot = new InputSnapshot
            {
                Move = Vector2.ClampMagnitude(move, 1f),
                Look = look,
                // 押下した瞬間のみtrue（エッジ検出）
                JumpPressed = jumpHeld && !_prevJump,
                AttackPressed = attackHeld && !_prevAttack,
                DashPressed = dashHeld && !_prevDash,
                InteractPressed = interactHeld && !_prevInteract,
                Frame = Time.frameCount,
                Time = Time.time,
            };

            _prevJump = jumpHeld;
            _prevAttack = attackHeld;
            _prevDash = dashHeld;
            _prevInteract = interactHeld;

            return snapshot;
        }
    }
}
