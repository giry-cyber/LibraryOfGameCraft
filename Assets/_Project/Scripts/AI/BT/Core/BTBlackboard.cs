using System.Collections.Generic;

namespace LibraryOfGamecraft.BT
{
    public class BTBlackboard
    {
        private readonly Dictionary<string, object> _data = new();

        public void Set<T>(string key, T value) => _data[key] = value;

        public T Get<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out var val) && val is T typed)
                return typed;
            return defaultValue;
        }

        public bool Has(string key) => _data.ContainsKey(key);

        public void Unset(string key) => _data.Remove(key);

        public void Clear() => _data.Clear();
    }
}
