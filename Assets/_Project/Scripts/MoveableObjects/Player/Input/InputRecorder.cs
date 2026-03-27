using System.Collections.Generic;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// 入力履歴を記録する。リプレイ・デバッグ再現・AI入力・ネットワーク同期に使用する。
    /// </summary>
    public class InputRecorder
    {
        private readonly List<InputSnapshot> _history = new List<InputSnapshot>();
        private readonly int _maxHistory;

        public IReadOnlyList<InputSnapshot> History => _history;

        public InputRecorder(int maxHistory = 600)
        {
            _maxHistory = maxHistory;
        }

        public void Record(InputSnapshot snapshot)
        {
            _history.Add(snapshot);
            if (_history.Count > _maxHistory)
                _history.RemoveAt(0);
        }

        public void Clear() => _history.Clear();
    }
}
