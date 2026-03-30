using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// プレイヤーキャラクター制御のエントリーポイント。
    /// 各モジュールを組み立て、更新順を制御する。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("設定 (ScriptableObject)")]
        [SerializeField] private MovementTuning _movementTuning;
        [SerializeField] private JumpSettings _jumpSettings;
        [SerializeField] private GravitySettings _gravitySettings;

        [Header("参照")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _cameraTransform;

        private PlayerInputReader _inputReader;
        private InputRecorder _inputRecorder;
        private CharacterBrain _brain;
        private CharacterStateContext _context;
        private CharacterStateMachine _stateMachine;
        private CharacterControllerMotor _motor;
        private PlayerAnimatorAdapter _animatorAdapter;

        public PlayerEventSystem Events { get; private set; }
        public ICharacterMotor Motor => _motor;
        public CharacterStateType CurrentState => _stateMachine.CurrentStateType;

        private void Awake()
        {
            var cc = GetComponent<CharacterController>();

            Events = new PlayerEventSystem();
            _motor = new CharacterControllerMotor(cc, _movementTuning, _gravitySettings);
            _inputReader = new PlayerInputReader();
            _inputRecorder = new InputRecorder();
            _brain = new CharacterBrain();

            _context = new CharacterStateContext
            {
                Motor = _motor,
                Events = Events,
                MovementTuning = _movementTuning,
                JumpSettings = _jumpSettings,
                GravitySettings = _gravitySettings,
                CameraTransform = _cameraTransform,
            };

            _stateMachine = new CharacterStateMachine();
            _context.StateMachine = _stateMachine;
            _stateMachine.Initialize(_context, CharacterStateType.Idle);

            _animatorAdapter = new PlayerAnimatorAdapter(_animator);
        }

        private void Update()
        {
            // 接地情報を最初に更新し、Brain・StateMachine が今フレームの値を参照できるようにする
            _motor.RefreshGroundInfo();

            var snapshot = _inputReader.ReadSnapshot();
            _inputRecorder.Record(snapshot);
            _context.CurrentInput = snapshot;

            // Brain が入力を解釈してコマンドを発行
            _brain.Process(snapshot, _context);

            // 現在の State を Tick（遷移判定と内部処理）
            _stateMachine.Tick(Time.deltaTime);

            // 現在の State の MovementStrategy で水平速度を計算してモーターに渡す
            var strategy = _stateMachine.CurrentState?.MovementStrategy;
            if (strategy != null)
                _motor.HorizontalVelocity = strategy.ComputeVelocity(_context, Time.deltaTime);

            // 重力・GroundSnap・実際の移動を適用
            _motor.Tick(Time.deltaTime);

            // 進行方向へキャラクターを回転
            RotateTowardsMoveDirection();

            // Animator パラメータを更新（Speed は MoveSpeed で正規化して 0～1 で渡す）
            var normalizedSpeed = _motor.HorizontalVelocity.magnitude / _movementTuning.MoveSpeed;
            _animatorAdapter.Update(_motor, _stateMachine, normalizedSpeed);
        }

        private void RotateTowardsMoveDirection()
        {
            var vel = _motor.HorizontalVelocity;
            if (vel.sqrMagnitude < 0.01f) return;

            var targetRot = Quaternion.LookRotation(vel.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.deltaTime);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            _motor.HandleControllerCollision(hit);
        }

        /// <summary>外部から Stun を付与する（ダメージ処理等から呼ぶ）。</summary>
        public void ApplyStun(float duration)
        {
            var stunState = _stateMachine.GetStunState();
            stunState.Initialize(duration);
            _stateMachine.ForceTransition(CharacterStateType.Stun);
        }
    }
}
