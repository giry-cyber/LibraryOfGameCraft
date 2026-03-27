using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// プレイヤーのデバッグ情報を可視化するコンポーネント。
    /// 接地判定の SphereCast 範囲と現在ステートをリアルタイムで表示する。
    /// PlayerController と同一 GameObject にアタッチして使う。
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerDebugger : MonoBehaviour
    {
        [SerializeField] private bool _showDebug = true;
        [SerializeField] private MovementTuning _movementTuning;
        [SerializeField] private Vector2 _guiOffset = new Vector2(10f, 10f);

        private PlayerController _controller;
        private CharacterController _cc;

        // ステートごとのラベル色
        private static readonly Color ColIdle    = new Color(0.4f, 0.9f, 0.4f);
        private static readonly Color ColMove    = new Color(0.3f, 0.7f, 1.0f);
        private static readonly Color ColJump    = new Color(1.0f, 0.9f, 0.2f);
        private static readonly Color ColFall    = new Color(1.0f, 0.5f, 0.1f);
        private static readonly Color ColLanding = new Color(0.8f, 0.4f, 1.0f);
        private static readonly Color ColAttack  = new Color(1.0f, 0.3f, 0.3f);
        private static readonly Color ColDodge   = new Color(0.2f, 0.9f, 0.9f);
        private static readonly Color ColStun    = new Color(0.6f, 0.6f, 0.6f);

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _cc = GetComponent<CharacterController>();
        }

        // ────────────────────────────────────────────
        // Screen overlay
        // ────────────────────────────────────────────
        private void OnGUI()
        {
            if (!_showDebug || _controller == null) return;

            var ground = _controller.Motor.GroundInfo;
            var state  = _controller.CurrentState;
            float vertVel  = _controller.Motor.VerticalVelocity;
            float horizSpd = _controller.Motor.HorizontalVelocity.magnitude;

            const float W     = 250f;
            const float LineH = 22f;
            const int   Lines = 6;
            float x = _guiOffset.x;
            float y = _guiOffset.y;

            GUI.Box(new Rect(x, y, W, LineH * Lines + 14f), GUIContent.none);

            Color prev = GUI.color;
            float lx = x + 8f;
            float ly = y + 7f;

            // ステート名
            GUI.color = StateColor(state);
            GUI.Label(new Rect(lx, ly, W, LineH), $"State : {state}");
            ly += LineH;
            GUI.color = prev;

            // 接地
            GUI.color = ground.IsGrounded ? Color.green : Color.red;
            GUI.Label(new Rect(lx, ly, W, LineH), $"Grounded : {ground.IsGrounded}");
            ly += LineH;
            GUI.color = prev;

            // 速度
            GUI.Label(new Rect(lx, ly, W, LineH), $"VertVel  : {vertVel:+0.00;-0.00} m/s");
            ly += LineH;
            GUI.Label(new Rect(lx, ly, W, LineH), $"HorizSpd : {horizSpd:F2} m/s");
            ly += LineH;

            // 斜面 / 距離
            if (ground.IsGrounded)
            {
                GUI.Label(new Rect(lx, ly, W, LineH), $"Slope    : {ground.SlopeAngle:F1}°");
                ly += LineH;
                GUI.Label(new Rect(lx, ly, W, LineH), $"GroundDist: {ground.GroundDistance:F3} m");
            }
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUI.Label(new Rect(lx, ly, W, LineH), "Slope    : --");
                ly += LineH;
                GUI.Label(new Rect(lx, ly, W, LineH), "GroundDist: --");
                GUI.color = prev;
            }
        }

        // ────────────────────────────────────────────
        // Gizmos（シーンビュー + プレイ中）
        // ────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            if (!_showDebug || _cc == null) return;

            // SphereCast の起点（カプセル底面の球体中心）
            var origin = transform.position
                + _cc.center
                - Vector3.up * (_cc.height / 2f - _cc.radius);

            float sphereR       = _cc.radius * 0.9f;
            float checkDist     = _movementTuning != null ? _movementTuning.GroundCheckDistance : 0.15f;
            float threshold     = _movementTuning != null ? _movementTuning.GroundedThreshold   : 0.05f;

            bool grounded = Application.isPlaying && _controller != null
                && _controller.Motor.GroundInfo.IsGrounded;

            // 起点球 — 接地:緑 / 空中:赤
            Color baseCol = grounded ? Color.green : Color.red;
            Gizmos.color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.25f);
            Gizmos.DrawSphere(origin, sphereR);
            Gizmos.color = baseCol;
            Gizmos.DrawWireSphere(origin, sphereR);

            // 判定終端球（GroundCheckDistance 先）
            var endOrigin = origin + Vector3.down * checkDist;
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawWireSphere(endOrigin, sphereR);

            // SphereCast の軸ライン
            Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
            Gizmos.DrawLine(origin, endOrigin);

            // GroundedThreshold ライン（シアン）
            Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
            Gizmos.DrawLine(origin, origin + Vector3.down * threshold);

            // 接地ヒット点 + 法線
            if (Application.isPlaying && _controller != null)
            {
                var g = _controller.Motor.GroundInfo;
                if (g.IsGrounded)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(g.GroundPoint, 0.04f);

                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(g.GroundPoint, g.GroundPoint + g.GroundNormal * 0.5f);
                }
            }
        }

        private static Color StateColor(CharacterStateType s) => s switch
        {
            CharacterStateType.Idle    => ColIdle,
            CharacterStateType.Move    => ColMove,
            CharacterStateType.Jump    => ColJump,
            CharacterStateType.Fall    => ColFall,
            CharacterStateType.Landing => ColLanding,
            CharacterStateType.Attack  => ColAttack,
            CharacterStateType.Dodge   => ColDodge,
            CharacterStateType.Stun    => ColStun,
            _                          => Color.white,
        };
    }
}
