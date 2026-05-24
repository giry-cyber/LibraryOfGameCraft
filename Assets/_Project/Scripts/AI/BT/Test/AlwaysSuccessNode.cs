using UnityEngine;

namespace LibraryOfGamecraft.BT.Test
{
    [CreateAssetMenu(fileName = "AlwaysSuccess", menuName = "LibraryOfGamecraft/BT/Test/AlwaysSuccess")]
    public class AlwaysSuccessNode : BTNode
    {
        public static AlwaysSuccessNode Create() => CreateInstance<AlwaysSuccessNode>();

        public override BTStatus Tick(BTContext ctx) => BTStatus.Success;
    }
}
