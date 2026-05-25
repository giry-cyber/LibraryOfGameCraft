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

        // PerceptionSensor など外部コンポーネントが Blackboard を読み書きできるように公開する
        public BTBlackboard Blackboard => _context?.Blackboard;

        protected virtual void Awake()
        {
            _context = new BTContext(gameObject);
        }

        protected virtual void Start()
        {
            // スポーン位置を HomePosition として記録（WanderAction などが参照）
            _context.Blackboard.Set(BTKeys.HomePosition, transform.position);

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
