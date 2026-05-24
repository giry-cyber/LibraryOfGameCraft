using UnityEngine;

namespace LibraryOfGamecraft.BT.Test
{
    [CreateAssetMenu(fileName = "AlwaysFailure", menuName = "LibraryOfGamecraft/BT/Test/AlwaysFailure")]
    public class AlwaysFailureNode : BTNode
    {
        public static AlwaysFailureNode Create() => CreateInstance<AlwaysFailureNode>();

        public override BTStatus Tick(BTContext ctx) => BTStatus.Failure;
    }
}
