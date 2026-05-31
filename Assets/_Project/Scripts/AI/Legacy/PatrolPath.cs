using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    // シーンに配置するウェイポイント列。PatrolNode から AIController 経由で参照する。
    public class PatrolPath : MonoBehaviour
    {
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private Color _gizmoColor = Color.cyan;

        public Transform[] Waypoints => _waypoints;

        private void OnDrawGizmos()
        {
            if (_waypoints == null || _waypoints.Length == 0) return;

            Gizmos.color = _gizmoColor;
            for (var i = 0; i < _waypoints.Length; i++)
            {
                if (_waypoints[i] == null) continue;
                Gizmos.DrawSphere(_waypoints[i].position, 0.3f);

                var next = _waypoints[(i + 1) % _waypoints.Length];
                if (next != null)
                    Gizmos.DrawLine(_waypoints[i].position, next.position);
            }
        }
    }
}
