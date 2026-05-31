using System;
using UnityEngine;
using UnityEngine.AI;

namespace LibraryOfGamecraft.AI
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIController : MonoBehaviour
    {
        [SerializeField] private AIBehaviourGraph _graph;
        [SerializeField] private PatrolPath _patrolPath;
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _stoppingDistance = 0.5f;

        private CharacterController _characterController;
        private NavMeshAgent _agent;
        private AINode _currentNode;
        private float _verticalVelocity;

        public AIBlackboard Blackboard { get; } = new AIBlackboard();
        public event Action<AINode> OnNodeEntered;

        // AttackNode が攻撃を発火するとき呼ぶ（ダメージ処理は外部が購読）
        public event Action OnAttackTriggered;

        // ノードから参照するコンテキスト情報
        public Transform SelfTransform => transform;
        public Vector3 HomePosition { get; private set; }
        public float ElapsedTimeInState { get; private set; }
        public bool IsGrounded => _characterController.isGrounded;
        public float WalkSpeed => _moveSpeed;   // インスペクタで設定したデフォルト速度
        public PatrolPath PatrolPath => _patrolPath;
        public Transform TargetTransform { get => _targetTransform; set => _targetTransform = value; }

        // XZ 平面の移動速度ベクトル（AIAnimatorAdapter でブレンドツリーに渡す）
        public Vector3 Velocity => new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);

        // NavMeshAgent が目標に到達したか
        public bool HasArrived =>
            _agent.isOnNavMesh &&
            !_agent.pathPending &&
            _agent.hasPath &&
            _agent.remainingDistance <= _agent.stoppingDistance + 0.1f;

        // 経路が完全に計算されているか（Partial/Invalid はパス失敗）
        public bool IsPathValid =>
            _agent.isOnNavMesh &&
            _agent.hasPath &&
            _agent.pathStatus == NavMeshPathStatus.PathComplete;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _agent = GetComponent<NavMeshAgent>();

            // 移動・回転は CharacterController が担当するので Agent の自動制御を無効化
            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.speed = _moveSpeed;
            _agent.stoppingDistance = _stoppingDistance;

            // CC と Agent の半径を合わせる（ずれると壁にぶつかる原因になる）
            _agent.radius = _characterController.radius;
        }

        private void Start()
        {
            HomePosition = transform.position;
            if (_graph == null)
            {
                Debug.LogWarning($"[AIController] {name}: Graph が未設定です", this);
                return;
            }
            if (_graph.StartNode == null)
            {
                Debug.LogWarning($"[AIController] {name}: Graph の StartNode が未設定です", this);
                return;
            }
            TransitionTo(_graph.StartNode);
        }

        private void Update()
        {
            if (_currentNode == null) return;

            ElapsedTimeInState += Time.deltaTime;
            _currentNode.Tick(this);
            CheckTransitions();
            ApplyMovement();
        }

        private void CheckTransitions()
        {
            foreach (var transition in _currentNode.Transitions)
            {
                if (transition.Condition == null || transition.Condition.Evaluate(this))
                {
                    TransitionTo(transition.NextNode);
                    return;
                }
            }
        }

        public void TransitionTo(AINode next)
        {
            _currentNode?.OnExit(this);
            _currentNode = next;
            ElapsedTimeInState = 0f;
            _currentNode?.OnEnter(this);
            OnNodeEntered?.Invoke(next);
            Debug.Log($"[AIController] {name}: → {(next != null ? next.name : "null（ノードなし）")}", this);
        }

        // ノードから呼ぶ移動API
        public void SetMoveSpeed(float speed)
        {
            _agent.speed = speed;
        }

        // ノードからターゲット方向へ向く（停止中の AlertNode / AttackNode で使用）
        public void FaceToward(Vector3 target, float speed)
        {
            var dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
        }

        public void TriggerAttack() => OnAttackTriggered?.Invoke();

        public void SetDestination(Vector3 destination)
        {
            if (!_agent.isOnNavMesh)
            {
                Debug.LogWarning($"[AIController] {name}: NavMesh 上にいません。SetDestination をスキップします。", this);
                return;
            }
            _agent.SetDestination(destination);
        }

        public void StopMovement()
        {
            if (_agent.isOnNavMesh)
                _agent.ResetPath();
        }

        private void ApplyMovement()
        {
            if (_characterController.isGrounded)
                _verticalVelocity = -0.5f;
            else
                _verticalVelocity -= 9.81f * Time.deltaTime;

            // NavMeshAgent が計算した経路方向を取得（障害物考慮済み）
            var horizontal = _agent.desiredVelocity;
            var motion = new Vector3(horizontal.x, _verticalVelocity, horizontal.z);
            _characterController.Move(motion * Time.deltaTime);

            // Agent の内部位置を CharacterController に同期（ずれ防止）
            _agent.nextPosition = transform.position;

            if (horizontal.sqrMagnitude > 0.01f)
            {
                var targetRotation = Quaternion.LookRotation(horizontal);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
            }
        }
    }
}
