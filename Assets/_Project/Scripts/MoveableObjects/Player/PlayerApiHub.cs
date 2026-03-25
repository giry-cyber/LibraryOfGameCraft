using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// Player 全体の参照ハブ（API Hub）。
    ///
    /// 役割：
    ///   1. 各コンポーネントへの参照を一元管理する
    ///   2. Inspector の設定値を他クラスに公開する
    ///   3. 依存関係を PlayerApiHub に集約し、クラス間の直接結合を避ける
    ///
    /// 設計方針（Open/Closed Principle）：
    ///   ・新しいコンポーネント（例: DashHandler）を追加する際は
    ///     このクラスに参照とアクセサを追加するだけでよい
    ///   ・既存コンポーネントの内部ロジックは変更不要
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerApiHub : MonoBehaviour
    {
        // ── Inspector 設定 ──────────────────────────────────────────────

        [Header("移動")]
        [Tooltip("目標移動速度 [m/s]")]
        [SerializeField] private float _moveSpeed = 5f;

        [Tooltip("PI コントローラが Rigidbody に加える最大水平力 [N]")]
        [SerializeField] private float _maxMoveForce = 50f;

        [Tooltip("水平方向の終端速度 [m/s]。この値から linearDamping を自動計算する。")]
        [SerializeField] private float _terminalHorizontalVelocity = 8f;

        [Header("PI 制御")]
        [Tooltip("積分ゲイン Ki。坂道や外力による定常偏差を除去する。大きすぎると振動する。")]
        [SerializeField] private float _ki = 1f;

        [Header("ジャンプ")]
        [Tooltip("ジャンプ時に加える力 [ForceMode.Impulse]")]
        [SerializeField] private float _jumpForce = 8f;

        [Tooltip("空中で追加できるジャンプ回数（2 段ジャンプは 1）")]
        [SerializeField] private int _maxExtraJumps = 1;

        [Header("カメラ")]
        [Tooltip("Main Camera の Transform。カメラ基準移動の方向計算に使用する。")]
        [SerializeField] private Transform _cameraTransform;

        [Header("接地判定")]
        [Tooltip("足元の判定球の半径。CapsuleCollider の半径に合わせること。")]
        [SerializeField] private float _groundCheckRadius = 0.28f;

        [Tooltip("判定球の底から地面までの許容距離。小さいほど厳密になる。")]
        [SerializeField] private float _groundCheckDistance = 0.05f;

        [Tooltip("地面として認識するレイヤー。Default を最低限含めること。")]
        [SerializeField] private LayerMask _groundLayer = -1;

        // ── コンポーネント参照（Awake で自動取得） ──────────────────────

        public Rigidbody      Rigidbody      { get; private set; }
        public InputReader    InputReader    { get; private set; }
        public MovementMotor  MovementMotor  { get; private set; }
        public GroundChecker  GroundChecker  { get; private set; }
        public JumpHandler    JumpHandler    { get; private set; }
        public PlayerAnimator PlayerAnimator { get; private set; }

        /// <summary>
        /// ステートマシンの参照。PlayerController.Start() から SetStateMachine() で登録される。
        /// Awake では生成できない（依存の順序制御が必要）ため遅延注入にしている。
        /// </summary>
        public PlayerStateMachine StateMachine { get; private set; }

        // ── 設定値アクセサ ──────────────────────────────────────────────

        public float     MoveSpeed                  => _moveSpeed;
        public float     MaxMoveForce               => _maxMoveForce;
        public float     TerminalHorizontalVelocity => _terminalHorizontalVelocity;
        public float     Ki                         => _ki;
        public float     JumpForce                  => _jumpForce;
        public int       MaxExtraJumps              => _maxExtraJumps;
        public float     GroundCheckRadius          => _groundCheckRadius;
        public float     GroundCheckDistance        => _groundCheckDistance;
        public LayerMask GroundLayer                => _groundLayer;
        public Transform CameraTransform           => _cameraTransform;

        // ── ライフサイクル ───────────────────────────────────────────────

        private void Awake()
        {
            Rigidbody     = GetComponent<Rigidbody>();
            InputReader   = GetComponent<InputReader>();
            MovementMotor = GetComponent<MovementMotor>();
            GroundChecker = GetComponent<GroundChecker>();
            JumpHandler   = GetComponent<JumpHandler>();
            PlayerAnimator = GetComponent<PlayerAnimator>();

            if (Rigidbody      == null) Debug.LogError("[PlayerApiHub] Rigidbody が見つかりません",      this);
            if (InputReader    == null) Debug.LogError("[PlayerApiHub] InputReader が見つかりません",    this);
            if (MovementMotor  == null) Debug.LogError("[PlayerApiHub] MovementMotor が見つかりません",  this);
            if (GroundChecker  == null) Debug.LogError("[PlayerApiHub] GroundChecker が見つかりません",  this);
            if (JumpHandler    == null) Debug.LogError("[PlayerApiHub] JumpHandler が見つかりません",    this);
            if (PlayerAnimator == null) Debug.LogError("[PlayerApiHub] PlayerAnimator が見つかりません", this);
        }

        // ── 公開メソッド ────────────────────────────────────────────────

        /// <summary>PlayerController.Start() からステートマシンを登録する。</summary>
        public void SetStateMachine(PlayerStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }
    }
}
