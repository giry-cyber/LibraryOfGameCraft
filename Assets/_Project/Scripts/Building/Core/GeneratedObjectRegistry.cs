using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    /// <summary>
    /// 生成済み GameObject と GeneratedElement の双方向マッピングを管理する。
    /// 再生成時の旧オブジェクト破棄・新オブジェクト登録を担う。
    /// </summary>
    public class GeneratedObjectRegistry : MonoBehaviour
    {
        private readonly Dictionary<int, GameObject> _elementToObject = new();
        private readonly Dictionary<int, int> _instanceIdToElement  = new();

        [SerializeField] private List<GeneratedElement> _elements = new();

        private Transform _generatedRoot;

        public Transform GeneratedRoot
        {
            get
            {
                EnsureGeneratedRoot();
                return _generatedRoot;
            }
        }

        public IReadOnlyList<GeneratedElement> Elements => _elements;

        private void EnsureGeneratedRoot()
        {
            if (_generatedRoot != null) return;

            var existing = transform.Find("GeneratedObjects");
            if (existing != null)
            {
                _generatedRoot = existing;
                return;
            }

            var go = new GameObject("GeneratedObjects");
            go.transform.SetParent(transform, false);
            _generatedRoot = go.transform;
        }

        public void Register(GeneratedElement element, GameObject go)
        {
            _elements.Add(element);
            _elementToObject[element.elementId]        = go;
            _instanceIdToElement[go.GetInstanceID()]   = element.elementId;
        }

        public GameObject GetObject(int elementId)
        {
            _elementToObject.TryGetValue(elementId, out var go);
            return go;
        }

        public int GetElementId(GameObject go)
        {
            _instanceIdToElement.TryGetValue(go.GetInstanceID(), out var id);
            return id;
        }

        /// <summary>
        /// 全生成オブジェクトを破棄してレジストリを空にする（Rebuild All 時に呼ぶ）。
        /// </summary>
        public void Clear()
        {
            EnsureGeneratedRoot();

            var children = new List<GameObject>();
            foreach (Transform child in _generatedRoot)
                children.Add(child.gameObject);

            foreach (var child in children)
                Object.DestroyImmediate(child);

            _elementToObject.Clear();
            _instanceIdToElement.Clear();
            _elements.Clear();
        }

        private static int _nextId;

        public static int IssueElementId() => ++_nextId;
    }
}
