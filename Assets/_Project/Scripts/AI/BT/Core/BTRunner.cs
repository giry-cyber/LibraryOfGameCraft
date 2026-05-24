using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // BT を毎フレーム駆動する MonoBehaviour。
    // Phase1: SetRoot() でコードからツリーをセット。
    // Phase2: BTGraph アセットをインスペクタでアサインする形に移行予定。
    public class BTRunner : MonoBehaviour
    {
        private BTContext _context;
        private BTNode    _root;

        protected virtual void Awake()
        {
            _context = new BTContext(gameObject);
        }

        public void SetRoot(BTNode root) => _root = root;

        private void Update()
        {
            if (_root == null) return;
            var status = _root.Tick(_context);
            // デバッグ用: ルートの結果をログに出したい場合は下記をコメント解除
            // Debug.Log($"[BTRunner] {name}: {status}");
        }
    }
}
