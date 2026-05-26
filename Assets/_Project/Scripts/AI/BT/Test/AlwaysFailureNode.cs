using UnityEngine;

namespace LibraryOfGamecraft.BT.Test
{
    [CreateAssetMenu(fileName = "AlwaysFailure", menuName = "LibraryOfGamecraft/BT/Test/AlwaysFailure")]
    public class AlwaysFailureNode : BTNode
    {
        public static AlwaysFailureNode Create() => CreateInstance<AlwaysFailureNode>();

        protected override BTStatus Execute(BTContext ctx) => BTStatus.Failure;
    }
}
