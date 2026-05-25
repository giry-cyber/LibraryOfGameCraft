using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // BT を毎フレーム駆動する MonoBehaviour。
    // BTGraph アセットをインスペクタでアサインするか、SetRoot() でコードからセットする。
    public class BTRunner : MonoBehaviour
    {
        [SerializeField] private BTGraph _graph;

        private BTContext _context;
        private BTNode    _root;

        protected virtual void Awake()
        {
            _context = new BTContext(gameObject);
        }

        protected virtual void Start()
        {
            if (_graph != null)
                _root = _graph.RootNode;
        }

        // コードからツリーをセットする場合（テスト・動的生成）
        public void SetRoot(BTNode root) => _root = root;

        private void Update()
        {
            if (_root == null) return;
            _root.Tick(_context);
        }
    }
}
