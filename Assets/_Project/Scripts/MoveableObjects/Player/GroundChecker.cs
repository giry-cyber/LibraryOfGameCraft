using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// 接地判定を担当するクラス。
    /// 足元から SphereCast を下に飛ばし、IsGrounded と法線を更新する。
    /// FixedUpdate で更新するため、物理判定と同期する。
    /// </summary>
    public class GroundChecker : MonoBehaviour
    {
        /// <summary>現在接地しているか</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>接地面の法線（斜面移動などに利用可能）</summary>
        public Vector3 GroundNormal { get; private set; } = Vector3.up;

        private PlayerApiHub _hub;

        private void Awake()
        {
            _hub = GetComponent<PlayerApiHub>();
        }

        private void FixedUpdate()
        {
            CheckGround();
        }

        private void CheckGround()
        {
            // SphereCast: 球の中心を足元より少し上に置き、下方向へキャストする
            // origin を radius 分だけ上にずらすことで、球がコライダーに埋まらないようにする
            Vector3 origin = transform.position + Vector3.up * _hub.GroundCheckRadius;
            float castDistance = _hub.GroundCheckRadius + _hub.GroundCheckDistance;

            bool hit = Physics.SphereCast(
                origin,
                _hub.GroundCheckRadius,
                Vector3.down,
                out RaycastHit hitInfo,
                castDistance,
                _hub.GroundLayer,
                QueryTriggerInteraction.Ignore
            );

            IsGrounded = hit;
            GroundNormal = hit ? hitInfo.normal : Vector3.up;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_hub == null) return;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 center = transform.position + Vector3.up * _hub.GroundCheckRadius;
            Gizmos.DrawWireSphere(center, _hub.GroundCheckRadius);
        }
#endif
    }
}
