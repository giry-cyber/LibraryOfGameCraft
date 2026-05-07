using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Events
{
    /// <summary>
    /// 汎用イベントチャンネル。ScriptableObject として作成し、
    /// 送信側は Raise()、受信側は GameEventListener で購読する。
    /// 会話・カメラ・アニメーション等、任意のシステム間で使用可能。
    /// </summary>
    [CreateAssetMenu(fileName = "GameEvent", menuName = "LibraryOfGamecraft/Events/GameEvent")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<GameEventListener> _listeners = new();

        /// <summary>このチャンネルを購読している全リスナーに通知する。</summary>
        public void Raise()
        {
            // リスナーが Raise 中に自己解除する場合があるため逆順でイテレート
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised();
        }

        public void Register(GameEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unregister(GameEventListener listener) =>
            _listeners.Remove(listener);

#if UNITY_EDITOR
        [ContextMenu("Raise (Debug)")]
        private void RaiseDebug() => Raise();
#endif
    }
}
