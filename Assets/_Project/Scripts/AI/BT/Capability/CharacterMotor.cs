using UnityEngine;
using UnityEngine.AI;

namespace LibraryOfGamecraft.BT
{
    // 移動・回転・重力を担う能力コンポーネント。
    // BT ノードはこのクラスを通じて移動を指示し、NavMeshAgent の詳細を意識しない。
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class CharacterMotor : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed       = 3f;
        [SerializeField] private float _rotationSpeed   = 10f;
        [SerializeField] private float _stoppingDistance = 0.5f;

        private CharacterController _cc;
        private NavMeshAgent        _agent;
        private float               _verticalVelocity;
        private int                 _debugFrames;

        // ── クエリ ──────────────────────────────────────────
        public float   DefaultSpeed => _moveSpeed;
        public Vector3 Velocity     => new Vector3(_cc.velocity.x, 0f, _cc.velocity.z);

        public bool HasArrived =>
            _agent.isOnNavMesh &&
            !_agent.pathPending &&
            _agent.hasPath &&
            _agent.remainingDistance <= _stoppingDistance + 0.1f;

        public bool IsPathValid =>
            _agent.isOnNavMesh &&
            _agent.hasPath &&
            _agent.pathStatus == NavMeshPathStatus.PathComplete;

        // ── コマンド ─────────────────────────────────────────
        public void MoveTo(Vector3 destination)
        {
            Debug.Log($"[CharacterMotor] {name}: MoveTo called / isOnNavMesh={_agent.isOnNavMesh} / dest={destination}");
            if (!_agent.isOnNavMesh)
            {
                Debug.LogWarning($"[CharacterMotor] {name}: NavMesh 上にいません", this);
                return;
            }

            // 目標座標を NavMesh 上に補正（空中や NavMesh 外の指定に対応）
            if (NavMesh.SamplePosition(destination, out var hit, 5f, NavMesh.AllAreas))
            {
                Debug.Log($"[CharacterMotor] {name}: SamplePosition 補正 {destination} → {hit.position}");
                _agent.SetDestination(hit.position);
            }
            else
            {
                Debug.LogWarning($"[CharacterMotor] {name}: 目標地点 {destination} の近くに NavMesh がありません（半径 5m 以内）", this);
                return;
            }

            _debugFrames = 10;
        }

        public void SetSpeed(float speed) => _agent.speed = speed;

        public void ResetSpeed() => _agent.speed = _moveSpeed;

        public void Stop()
        {
            if (_agent.isOnNavMesh) _agent.ResetPath();
        }

        public void FaceToward(Vector3 target, float speedOverride = -1f)
        {
            var dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            var speed = speedOverride > 0f ? speedOverride : _rotationSpeed;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
        }

        // ── Unity ライフサイクル ──────────────────────────────
        private void Awake()
        {
            _cc    = GetComponent<CharacterController>();
            _agent = GetComponent<NavMeshAgent>();

            _agent.updatePosition  = false;
            _agent.updateRotation  = false;
            _agent.speed           = _moveSpeed;
            _agent.stoppingDistance = _stoppingDistance;
            _agent.radius          = _cc.radius;
        }

        private void Update() => ApplyMovement();

        private void ApplyMovement()
        {
            if (_cc.isGrounded) _verticalVelocity = -0.5f;
            else                _verticalVelocity -= 9.81f * Time.deltaTime;

            var horizontal = _agent.desiredVelocity;

            if (_debugFrames > 0)
            {
                Debug.Log($"[CharacterMotor] {name}: desiredVelocity={horizontal} / pathPending={_agent.pathPending} / hasPath={_agent.hasPath} / remaining={_agent.remainingDistance:F2}");
                _debugFrames--;
            }

            _cc.Move(new Vector3(horizontal.x, _verticalVelocity, horizontal.z) * Time.deltaTime);
            _agent.nextPosition = transform.position;

            if (horizontal.sqrMagnitude > 0.01f)
            {
                var targetRot = Quaternion.LookRotation(horizontal);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot, Time.deltaTime * _rotationSpeed);
            }
        }
    }
}
