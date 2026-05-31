using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "ChaseNode", menuName = "LibraryOfGamecraft/AI/Nodes/ChaseNode")]
    public class ChaseNode : AINode
    {
        private const string KeyNextUpdate = "chase_nextUpdate";

        [SerializeField] private float _chaseSpeed     = 5f;    // 追跡時の速さ（WalkSpeed より速く設定する）
        [SerializeField] private float _updateInterval = 0.3f;  // 経路再計算の間隔（秒）
        [SerializeField] private float _giveUpDistance = 20f;   // この距離以上離れたら追跡を断念

        public float GiveUpDistance => _giveUpDistance;

        public override void OnEnter(AIController context)
        {
            context.SetMoveSpeed(_chaseSpeed);
            context.Blackboard.Set(KeyNextUpdate, 0f);
            UpdateDestination(context);
            Debug.Log($"[ChaseNode] {context.SelfTransform.name}: 追跡開始");
        }

        public override void Tick(AIController context)
        {
            if (context.TargetTransform == null) return;

            var now = Time.time;
            if (now >= context.Blackboard.Get<float>(KeyNextUpdate))
            {
                context.Blackboard.Set(KeyNextUpdate, now + _updateInterval);
                UpdateDestination(context);
            }
        }

        public override void OnExit(AIController context)
        {
            context.SetMoveSpeed(context.WalkSpeed);
            context.StopMovement();
        }

        private void UpdateDestination(AIController context)
        {
            if (context.TargetTransform == null) return;
            context.SetDestination(context.TargetTransform.position);
        }
    }
}
