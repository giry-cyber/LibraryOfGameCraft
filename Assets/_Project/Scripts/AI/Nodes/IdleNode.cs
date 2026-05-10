using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "IdleNode", menuName = "LibraryOfGamecraft/AI/Nodes/IdleNode")]
    public class IdleNode : AINode
    {
        public override void OnEnter(AIController context)
        {
            context.DesiredMoveDirection = Vector3.zero;
        }

        public override void Tick(AIController context)
        {
            context.DesiredMoveDirection = Vector3.zero;
        }

        public override void OnExit(AIController context) { }
    }
}
