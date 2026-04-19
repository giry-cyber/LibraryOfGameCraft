using System.Collections.Generic;

namespace LibraryOfGamecraft.Dialogue
{
    public class DialogueFlagService
    {
        private readonly Dictionary<string, bool> _boolFlags = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _intFlags = new Dictionary<string, int>();

        public void Initialize(DialogueFlagDatabase database)
        {
            if (database == null) return;
            foreach (var flag in database.Flags)
            {
                if (flag.FlagType == FlagType.Bool)
                    _boolFlags[flag.FlagId] = flag.DefaultBoolValue;
                else
                    _intFlags[flag.FlagId] = flag.DefaultIntValue;
            }
        }

        public bool GetBool(string flagId) =>
            _boolFlags.TryGetValue(flagId, out var v) ? v : false;

        public int GetInt(string flagId) =>
            _intFlags.TryGetValue(flagId, out var v) ? v : 0;

        public void SetBool(string flagId, bool value) => _boolFlags[flagId] = value;

        public void SetInt(string flagId, int value) => _intFlags[flagId] = value;

        public void AddInt(string flagId, int delta) =>
            _intFlags[flagId] = GetInt(flagId) + delta;

        // セーブ/ロード用
        public Dictionary<string, bool> GetBoolFlagsCopy() => new Dictionary<string, bool>(_boolFlags);
        public Dictionary<string, int> GetIntFlagsCopy() => new Dictionary<string, int>(_intFlags);

        public void RestoreBoolFlags(Dictionary<string, bool> data)
        {
            foreach (var kv in data) _boolFlags[kv.Key] = kv.Value;
        }

        public void RestoreIntFlags(Dictionary<string, int> data)
        {
            foreach (var kv in data) _intFlags[kv.Key] = kv.Value;
        }
    }
}
