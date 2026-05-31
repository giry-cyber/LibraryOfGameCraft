using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "ReturnNode", menuName = "LibraryOfGamecraft/AI/Nodes/ReturnNode")]
    public class ReturnNode : AINode
    {
        public override void OnEnter(AIController context)
        {
            context.SetDestination(context.HomePosition);
            Debug.Log($"[ReturnNode] {context.SelfTransform.name}: HomePosition={context.HomePosition} へ帰還");
        }

        public override void Tick(AIController context)
        {
            // 到達判定は HasArrivedCondition で行う
        }

        public override void OnExit(AIController context)
        {
            context.StopMovement();
        }
    }
}
