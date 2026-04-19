using System.Collections.Generic;

namespace LibraryOfGamecraft.Dialogue
{
    public class DialogueHistoryService
    {
        private readonly HashSet<string> _readNodeIds = new HashSet<string>();
        private readonly List<DialogueLogEntry> _sessionLog = new List<DialogueLogEntry>();
        private int _logOrder;

        public bool IsRead(string nodeId) => _readNodeIds.Contains(nodeId);

        public void MarkRead(string nodeId) => _readNodeIds.Add(nodeId);

        public void AddLog(string speakerName, string text) =>
            _sessionLog.Add(new DialogueLogEntry(_logOrder++, speakerName, text));

        public IReadOnlyList<DialogueLogEntry> GetSessionLog() => _sessionLog;

        public void ClearSessionLog()
        {
            _sessionLog.Clear();
            _logOrder = 0;
        }

        // セーブ/ロード用
        public HashSet<string> GetReadNodeIdsCopy() => new HashSet<string>(_readNodeIds);

        public void RestoreReadNodeIds(HashSet<string> data)
        {
            foreach (var id in data) _readNodeIds.Add(id);
        }
    }
}
