using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // 全子が Success を返したら Success（AND）。
    // 毎 Tick 先頭から評価するため、条件ノードが常に再チェックされる。
    // アクションの継続状態は BTAction が自身の Blackboard エントリで管理する。
    [CreateAssetMenu(fileName = "BTSequence", menuName = "LibraryOfGamecraft/BT/Composites/Sequence")]
    public class BTSequence : BTComposite
    {
        public override BTStatus Tick(BTContext ctx)
        {
            foreach (var child in _children)
            {
                var status = child.Tick(ctx);
                if (status != BTStatus.Success) return status;
            }
            return BTStatus.Success;
        }
    }
}
