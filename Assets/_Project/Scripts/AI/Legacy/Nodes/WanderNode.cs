using UnityEngine;
using UnityEngine.AI;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "WanderNode", menuName = "LibraryOfGamecraft/AI/Nodes/WanderNode")]
    public class WanderNode : AINode
    {
        private const string KeyHasTarget  = "wander_hasTarget";
        private const string KeyTimeInMove = "wander_timeInMove";

        [SerializeField] private float _wanderRadius = 5f;
        [SerializeField] private float _moveTimeout  = 8f;   // この秒数内に到達できなければ諦める

        public float WanderRadius => _wanderRadius;

        public override void OnEnter(AIController context)
        {
            Debug.Log($"[WanderNode] {context.SelfTransform.name}: OnEnter / HomePosition={context.HomePosition} / Radius={_wanderRadius}");
            context.Blackboard.Set(KeyHasTarget, false);
            PickNewTarget(context);
        }

        public override void Tick(AIController context)
        {
            if (!context.Blackboard.Get<bool>(KeyHasTarget))
            {
                PickNewTarget(context);
                return;
            }

            // 到達
            if (context.HasArrived)
            {
                context.Blackboard.Set(KeyHasTarget, false);
                return;
            }

            // 経路失敗（壁の中など到達不可能な目標）→ 即座に選び直す
            if (!context.IsPathValid)
            {
                Debug.LogWarning($"[WanderNode] {context.SelfTransform.name}: 経路失敗、ターゲットを選び直します");
                context.Blackboard.Set(KeyHasTarget, false);
                return;
            }

            // タイムアウト（長時間到達できていない）→ 諦めて選び直す
            var elapsed = context.Blackboard.Get<float>(KeyTimeInMove) + Time.deltaTime;
            context.Blackboard.Set(KeyTimeInMove, elapsed);
            if (elapsed >= _moveTimeout)
            {
                Debug.LogWarning($"[WanderNode] {context.SelfTransform.name}: タイムアウト、ターゲットを選び直します");
                context.Blackboard.Set(KeyHasTarget, false);
            }
        }

        public override void OnExit(AIController context)
        {
            context.StopMovement();
            context.Blackboard.Set(KeyHasTarget, false);
        }

        private void PickNewTarget(AIController context)
        {
            var candidate = context.HomePosition + new Vector3(
                Random.Range(-_wanderRadius, _wanderRadius),
                0f,
                Random.Range(-_wanderRadius, _wanderRadius)
            );

            if (NavMesh.SamplePosition(candidate, out var hit, _wanderRadius, NavMesh.AllAreas))
            {
                context.SetDestination(hit.position);
                context.Blackboard.Set(KeyHasTarget, true);
                context.Blackboard.Set(KeyTimeInMove, 0f);
                Debug.Log($"[WanderNode] {context.SelfTransform.name}: 新ターゲット={hit.position}");
            }
            else
            {
                Debug.LogWarning($"[WanderNode] {context.SelfTransform.name}: NavMesh 上に有効な点が見つかりません（候補={candidate}）");
            }
        }
    }
}
