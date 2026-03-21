using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// Rigidbody への水平移動を担当するクラス。
    /// PI 制御で目標速度へ追従し、線形抵抗で終端速度を自然に制限する。
    ///
    /// ══════════════════════════════════════════════════════
    ///  物理モデル：線形抵抗
    /// ══════════════════════════════════════════════════════
    /// Unity の Rigidbody.linearDamping は毎物理フレームに以下を適用する：
    ///
    ///   v_new = v_old × max(0, 1 - drag × Δt)
    ///
    /// 微小時間での近似として、これは線形抵抗力：
    ///
    ///   F_drag = -mass × drag × v
    ///
    /// と等価。終端速度 v_t に達したとき合力 = 0 なので：
    ///
    ///   F_applied = mass × drag × v_t
    ///   → drag = F_applied / (mass × v_t)
    ///
    /// ここで F_applied = maxMoveForce（PI コントローラの出力上限）。
    ///
    /// ══════════════════════════════════════════════════════
    ///  PI 制御
    /// ══════════════════════════════════════════════════════
    ///   error     = v_target - v_current（移動方向への射影速度）
    ///   ∫error   += error × Δt
    ///   F         = Kp × error + Ki × ∫error
    ///   F         = Clamp(F, -maxMoveForce, +maxMoveForce)
    ///
    ///   Kp = maxMoveForce / terminalVelocity
    ///        （フル入力・ゼロ速度時に最大力を出す比例ゲイン）
    ///   Ki = Inspector で設定（坂道などでの定常偏差を除去する積分ゲイン）
    ///
    /// ─── PI を選んだ理由 ───────────────────────────────────
    ///   P のみ  → 坂道・外力で定常偏差が残る
    ///   I を加える → 定常偏差をゼロに収束できる
    ///   D は不使用 → 速度の数値微分はノイズが大きく、ゲームには不向き
    /// ══════════════════════════════════════════════════════
    /// </summary>
    public class MovementMotor : MonoBehaviour
    {
        private PlayerApiHub _hub;
        private Rigidbody    _rb;

        private Vector2 _moveInput;     // SetMoveInput() で更新される
        private float   _integralError; // PI の積分項

        private float _kp; // 比例ゲイン（Start で自動計算）

        /// <summary>現フレームの移動入力。状態クラスが遷移判定に使用する。</summary>
        public Vector2 CurrentInput => _moveInput;

        private void Awake()
        {
            _hub = GetComponent<PlayerApiHub>();
        }

        private void Start()
        {
            _rb = _hub.Rigidbody;
            InitializePhysics();
        }

        /// <summary>
        /// linearDamping と比例ゲイン Kp を自動計算して設定する。
        ///
        ///   drag = maxMoveForce / (mass × terminalVelocity)
        ///   Kp   = maxMoveForce / terminalVelocity
        /// </summary>
        private void InitializePhysics()
        {
            float mass  = _rb.mass;
            float vt    = _hub.TerminalHorizontalVelocity;
            float fMax  = _hub.MaxMoveForce;

            float drag = fMax / (mass * vt);
            _rb.linearDamping = drag;

            _kp = fMax / vt;

            Debug.Log($"[MovementMotor] 自動計算 → linearDamping={drag:F4}, Kp={_kp:F4}" +
                      $"  (maxForce={fMax}N, mass={mass}kg, vTerminal={vt}m/s)");
        }

        /// <summary>
        /// Update から呼ぶ。入力値を保持するだけで物理処理はしない。
        /// 物理適用は FixedUpdate の ApplyMovement() で行う。
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        /// <summary>
        /// FixedUpdate から呼ぶ。PI 制御で移動方向に力を加える。
        /// </summary>
        public void ApplyMovement()
        {
            if (_moveInput.sqrMagnitude < 0.01f)
            {
                _integralError = 0f; // 入力なし → 積分リセット（ワインドアップ防止）
                return;
            }

            // カメラ基準の移動方向を取得（カメラ未設定時はワールド基準にフォールバック）
            Vector3 moveDir = GetCameraRelativeMoveDir();

            // 移動方向への現在速度を射影で取得（横滑り分を除外）
            float currentSpeed = Vector3.Dot(_rb.linearVelocity, moveDir);

            // PI 制御
            float error = _hub.MoveSpeed - currentSpeed;
            _integralError += error * Time.fixedDeltaTime;
            float force = _kp * error + _hub.Ki * _integralError;
            force = Mathf.Clamp(force, -_hub.MaxMoveForce, _hub.MaxMoveForce);

            _rb.AddForce(moveDir * force, ForceMode.Force);

            // Rigidbody を進行方向へ向ける
            FaceDirection(moveDir);
        }

        /// <summary>
        /// 積分項をリセットする。IdleState 進入時などに呼ぶ。
        /// リセットしないと停止後の再加速に誤差が残ることがある。
        /// </summary>
        public void ResetIntegral()
        {
            _integralError = 0f;
        }

        /// <summary>
        /// カメラの向きを基準にした XZ 平面上の移動方向ベクトルを返す。
        ///
        ///   W キー → カメラが向いている方向へ進む
        ///   A キー → カメラの左方向へ進む
        ///
        /// _hub.CameraTransform が未設定の場合はワールド座標基準（従来の動作）にフォールバックする。
        /// </summary>
        private Vector3 GetCameraRelativeMoveDir()
        {
            if (_hub.CameraTransform == null)
                return new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

            // カメラの forward / right を XZ 平面に投影して正規化（坂の上を見ていても水平移動になる）
            Vector3 camForward = Vector3.ProjectOnPlane(_hub.CameraTransform.forward, Vector3.up).normalized;
            Vector3 camRight   = Vector3.ProjectOnPlane(_hub.CameraTransform.right,   Vector3.up).normalized;

            return (camForward * _moveInput.y + camRight * _moveInput.x).normalized;
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.01f) return;
            Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
            _rb.rotation = Quaternion.Slerp(_rb.rotation, target, 15f * Time.fixedDeltaTime);
        }
    }
}
