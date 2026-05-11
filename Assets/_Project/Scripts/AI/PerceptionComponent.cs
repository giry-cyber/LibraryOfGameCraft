using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    // 視野・聴覚でターゲットを検知し、AIController.TargetTransform を自動セット/クリアする。
    [RequireComponent(typeof(AIController))]
    public class PerceptionComponent : MonoBehaviour
    {
        [Header("視野")]
        [SerializeField] private float _viewDistance = 10f;
        [SerializeField] [Range(0f, 180f)] private float _viewHalfAngle = 60f;   // 片側角度
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private LayerMask _obstacleLayer;

        [Header("聴覚")]
        [SerializeField] private float _hearingRadius = 4f;

        [Header("パフォーマンス")]
        [SerializeField] private float _scanInterval = 0.2f;

        private AIController _aiController;
        private float _nextScan;

        private void Awake() => _aiController = GetComponent<AIController>();

        private void Update()
        {
            if (Time.time < _nextScan) return;
            _nextScan = Time.time + _scanInterval;
            Scan();
        }

        private void Scan()
        {
            // 目の高さを基点にする
            var origin = transform.position + Vector3.up * 1.6f;

            // 視野チェック（距離 → 角度 → 障害物の順で絞る）
            var hits = Physics.OverlapSphere(origin, _viewDistance, _targetLayer);
            foreach (var col in hits)
            {
                var toTarget = col.transform.position - origin;
                if (Vector3.Angle(transform.forward, toTarget) > _viewHalfAngle) continue;
                if (Physics.Raycast(origin, toTarget.normalized, toTarget.magnitude, _obstacleLayer)) continue;

                _aiController.TargetTransform = col.transform;
                return;
            }

            // 聴覚チェック（視野外でも近ければ検知）
            var hearingHits = Physics.OverlapSphere(origin, _hearingRadius, _targetLayer);
            if (hearingHits.Length > 0)
            {
                _aiController.TargetTransform = hearingHits[0].transform;
                return;
            }

            _aiController.TargetTransform = null;
        }

        private void OnDrawGizmosSelected()
        {
            var origin = transform.position + Vector3.up * 1.6f;

            // 視野扇形
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            var leftDir  = Quaternion.Euler(0, -_viewHalfAngle, 0) * transform.forward;
            var rightDir = Quaternion.Euler(0,  _viewHalfAngle, 0) * transform.forward;
            Gizmos.DrawRay(origin, leftDir  * _viewDistance);
            Gizmos.DrawRay(origin, rightDir * _viewDistance);
            Gizmos.DrawWireSphere(origin, _viewDistance);

            // 聴覚範囲
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireSphere(origin, _hearingRadius);
        }
    }
}
