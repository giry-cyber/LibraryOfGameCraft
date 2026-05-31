using UnityEngine;
using UnityEngine.AI;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "PatrolNode", menuName = "LibraryOfGamecraft/AI/Nodes/PatrolNode")]
    public class PatrolNode : AINode
    {
        private const string KeyWaypointIndex = "patrol_waypointIndex";

        public override void OnEnter(AIController context)
        {
            if (context.PatrolPath == null || context.PatrolPath.Waypoints.Length == 0)
            {
                Debug.LogWarning($"[PatrolNode] {context.SelfTransform.name}: PatrolPath が未設定または空です");
                return;
            }

            var index = context.Blackboard.Get<int>(KeyWaypointIndex, 0);
            MoveToWaypoint(context, index);
        }

        public override void Tick(AIController context)
        {
            if (context.PatrolPath == null || context.PatrolPath.Waypoints.Length == 0) return;

            if (!context.HasArrived) return;

            var waypoints = context.PatrolPath.Waypoints;
            var next = (context.Blackboard.Get<int>(KeyWaypointIndex, 0) + 1) % waypoints.Length;
            context.Blackboard.Set(KeyWaypointIndex, next);
            MoveToWaypoint(context, next);
        }

        public override void OnExit(AIController context)
        {
            context.StopMovement();
        }

        private void MoveToWaypoint(AIController context, int index)
        {
            var waypoints = context.PatrolPath.Waypoints;
            if (waypoints[index] == null)
            {
                Debug.LogWarning($"[PatrolNode] {context.SelfTransform.name}: waypoint[{index}] が null です");
                return;
            }

            var target = waypoints[index].position;
            if (NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas))
                context.SetDestination(hit.position);
            else
                context.SetDestination(target);

            Debug.Log($"[PatrolNode] {context.SelfTransform.name}: waypoint[{index}] へ移動 → {target}");
        }
    }
}
