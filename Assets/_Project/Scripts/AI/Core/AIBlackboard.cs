using System.Collections.Generic;

namespace LibraryOfGamecraft.AI
{
    // ノードはScriptableObjectなので共有インスタンスになる。
    // ノードごとのランタイム状態はすべてここに格納し、ノード自体はステートレスに保つ。
    public class AIBlackboard
    {
        private readonly Dictionary<string, object> _data = new();

        public T Get<T>(string key, T defaultValue = default)
        {
            return _data.TryGetValue(key, out var value) ? (T)value : defaultValue;
        }

        public void Set(string key, object value) => _data[key] = value;

        public void Clear() => _data.Clear();
    }
}
