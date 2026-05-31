using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "IdleNode", menuName = "LibraryOfGamecraft/AI/Nodes/IdleNode")]
    public class IdleNode : AINode
    {
        public override void OnEnter(AIController context) => context.StopMovement();
        public override void Tick(AIController context) { }
        public override void OnExit(AIController context) { }
    }
}
