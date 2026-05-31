using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // 全子が Success を返したら Success（AND）。
    // 毎 Tick 先頭から評価するため、条件ノードが常に再チェックされる。
    // アクションの継続状態は BTAction が自身の Blackboard エントリで管理する。
    [CreateAssetMenu(fileName = "BTSequence", menuName = "LibraryOfGamecraft/BT/Composites/Sequence")]
    public class BTSequence : BTComposite
    {
        protected override BTStatus Execute(BTContext ctx)
        {
            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i] == null) continue;
                var status = _children[i].Tick(ctx);
                if (status != BTStatus.Success)
                {
                    // 評価されなかった後続ノードを強制終了
                    for (int j = i + 1; j < _children.Count; j++)
                        if (_children[j] != null) _children[j].ForceExit(ctx);
                    return status;
                }
            }
            return BTStatus.Success;
        }
    }
}
