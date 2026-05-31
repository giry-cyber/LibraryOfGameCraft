using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // 最初に Success/Running を返した子で止まる（OR / 優先度選択）。
    // 毎 Tick 先頭から評価するため、高優先度ブランチへの割り込みが自然に機能する。
    [CreateAssetMenu(fileName = "BTSelector", menuName = "LibraryOfGamecraft/BT/Composites/Selector")]
    public class BTSelector : BTComposite
    {
        protected override BTStatus Execute(BTContext ctx)
        {
            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i] == null) continue;
                var status = _children[i].Tick(ctx);
                if (status != BTStatus.Failure)
                {
                    // 評価されなかった後続ノードを強制終了
                    for (int j = i + 1; j < _children.Count; j++)
                        if (_children[j] != null) _children[j].ForceExit(ctx);
                    return status;
                }
            }
            return BTStatus.Failure;
        }
    }
}
