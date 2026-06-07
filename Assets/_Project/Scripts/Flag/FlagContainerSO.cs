using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LibraryOfGamecraft.Flag
{
    [CreateAssetMenu(menuName = "LibraryOfGamecraft/Flag/FlagContainer", fileName = "FlagContainer")]
    public class FlagContainerSO : ScriptableObject
    {
        [SerializeField] private List<FlagEntry> _entries = new();

        private Dictionary<GameFlag, FlagEntry> _lookup = new();

        /// <summary>フラグの値が変化したときに発火。引数は (flag, newValue)。</summary>
        public event Action<GameFlag, bool> OnFlagChanged;

        private void OnEnable()
        {
            SyncWithEnum();
            ResetToDefault();
            BuildLookup();
        }

#if UNITY_EDITOR
        private void OnValidate() => SyncWithEnum();
#endif

        public bool Get(GameFlag flag)
        {
            return _lookup.TryGetValue(flag, out var entry) ? entry.RuntimeValue : false;
        }

        public void Set(GameFlag flag, bool value)
        {
            if (!_lookup.TryGetValue(flag, out var entry)) return;
            if (entry.RuntimeValue == value) return;
            entry.RuntimeValue = value;
            OnFlagChanged?.Invoke(flag, value);
        }

        public void Reset(GameFlag flag)
        {
            if (_lookup.TryGetValue(flag, out var entry))
                Set(flag, entry.DefaultValue);
        }

        public void ResetAll()
        {
            foreach (var flag in _lookup.Keys.ToList())
                Reset(flag);
        }

        // enumと_entriesリストを同期する。enumに追加されたフラグは自動追加、削除されたフラグは除去。
        private void SyncWithEnum()
        {
            var allFlags = (GameFlag[])Enum.GetValues(typeof(GameFlag));
            _entries.RemoveAll(e => !Enum.IsDefined(typeof(GameFlag), e.Flag));
            foreach (var flag in allFlags)
            {
                if (!_entries.Any(e => e.Flag == flag))
                    _entries.Add(new FlagEntry { Flag = flag });
            }
        }

        private void ResetToDefault()
        {
            foreach (var entry in _entries)
                entry.RuntimeValue = entry.DefaultValue;
        }

        private void BuildLookup()
        {
            _lookup = _entries.ToDictionary(e => e.Flag, e => e);
        }
    }
}
