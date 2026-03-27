using System;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// キャラクター状態変化を外部（SE・VFX・UI・カメラ等）へ通知するイベントシステム。
    /// </summary>
    public class PlayerEventSystem
    {
        public event Action OnJump;
        public event Action OnLand;
        public event Action OnAttackStart;
        public event Action<CharacterStateType> OnStateChanged;

        public void RaiseJump() => OnJump?.Invoke();
        public void RaiseLand() => OnLand?.Invoke();
        public void RaiseAttackStart() => OnAttackStart?.Invoke();
        public void RaiseStateChanged(CharacterStateType state) => OnStateChanged?.Invoke(state);
    }
}
