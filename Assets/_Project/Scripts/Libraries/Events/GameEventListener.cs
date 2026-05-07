using UnityEngine;
using UnityEngine.Events;

namespace LibraryOfGamecraft.Events
{
    /// <summary>
    /// GameEvent を購読し、発火時に UnityEvent を呼び出すコンポーネント。
    /// GameObject に付与して Inspector から応答処理を配線する。
    /// OnEnable/OnDisable で自動的に購読登録・解除を行う。
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [Tooltip("購読する GameEvent チャンネル")]
        public GameEvent Event;

        [Tooltip("イベント発火時に呼び出す処理")]
        public UnityEvent Response;

        private void OnEnable()
        {
            if (Event != null)
                Event.Register(this);
        }

        private void OnDisable()
        {
            if (Event != null)
                Event.Unregister(this);
        }

        public void OnEventRaised() => Response?.Invoke();
    }
}
