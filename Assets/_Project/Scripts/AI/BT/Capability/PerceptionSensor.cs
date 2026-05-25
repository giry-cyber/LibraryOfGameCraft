using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // 視野・聴覚でターゲットを検知し、Blackboard の BTKeys.Target を自動セット/クリアする。
    [RequireComponent(typeof(BTRunner))]
    public class PerceptionSensor : MonoBehaviour
    {
        [Header("視野")]
        [SerializeField] private float     _viewDistance  = 10f;
        [SerializeField] [Range(0f, 180f)] private float _viewHalfAngle = 60f;
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private LayerMask _obstacleLayer;

        [Header("聴覚")]
        [SerializeField] private float _hearingRadius = 4f;

        [Header("パフォーマンス")]
        [SerializeField] private float _scanInterval = 0.2f;

        private BTRunner _runner;
        private float    _nextScan;

        private void Awake() => _runner = GetComponent<BTRunner>();

        private void Update()
        {
            if (Time.time < _nextScan) return;
            _nextScan = Time.time + _scanInterval;
            Scan();
        }

        private void Scan()
        {
            var origin = transform.position + Vector3.up * 1.6f;

            // 視野チェック（距離 → 角度 → 障害物の順で絞る）
            var hits = Physics.OverlapSphere(origin, _viewDistance, _targetLayer);
            foreach (var col in hits)
            {
                var toTarget = col.transform.position - origin;
                if (Vector3.Angle(transform.forward, toTarget) > _viewHalfAngle) continue;
                if (Physics.Raycast(origin, toTarget.normalized, toTarget.magnitude, _obstacleLayer)) continue;

                _runner.Blackboard.Set(BTKeys.Target, col.transform);
                return;
            }

            // 聴覚チェック
            var hearingHits = Physics.OverlapSphere(origin, _hearingRadius, _targetLayer);
            if (hearingHits.Length > 0)
            {
                _runner.Blackboard.Set(BTKeys.Target, hearingHits[0].transform);
                return;
            }

            _runner.Blackboard.Unset(BTKeys.Target);
        }

        private void OnDrawGizmosSelected()
        {
            var origin = transform.position + Vector3.up * 1.6f;
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawRay(origin, Quaternion.Euler(0, -_viewHalfAngle, 0) * transform.forward * _viewDistance);
            Gizmos.DrawRay(origin, Quaternion.Euler(0,  _viewHalfAngle, 0) * transform.forward * _viewDistance);
            Gizmos.DrawWireSphere(origin, _viewDistance);
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireSphere(origin, _hearingRadius);
        }
    }
}
