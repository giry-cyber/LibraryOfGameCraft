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
            foreach (var child in _children)
            {
                var status = child.Tick(ctx);
                if (status != BTStatus.Failure) return status;
            }
            return BTStatus.Failure;
        }
    }
}
