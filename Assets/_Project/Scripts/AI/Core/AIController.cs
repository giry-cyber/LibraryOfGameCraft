using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [RequireComponent(typeof(CharacterController))]
    public class AIController : MonoBehaviour
    {
        [SerializeField] private AIBehaviourGraph _graph;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _rotationSpeed = 10f;

        private CharacterController _characterController;
        private AINode _currentNode;
        private float _verticalVelocity;

        public AIBlackboard Blackboard { get; } = new AIBlackboard();

        // ノードから参照するコンテキスト情報
        public Transform SelfTransform => transform;
        public Vector3 HomePosition { get; private set; }
        public float ElapsedTimeInState { get; private set; }

        // ノードが毎フレーム書き込む移動要求（XZ平面、正規化済みを想定）
        public Vector3 DesiredMoveDirection { get; set; }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            HomePosition = transform.position;
            if (_graph != null)
                TransitionTo(_graph.StartNode);
        }

        private void Update()
        {
            if (_currentNode == null) return;

            ElapsedTimeInState += Time.deltaTime;
            DesiredMoveDirection = Vector3.zero;

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
        }

        private void ApplyMovement()
        {
            if (_characterController.isGrounded)
                _verticalVelocity = -0.5f;
            else
                _verticalVelocity -= 9.81f * Time.deltaTime;

            var horizontal = DesiredMoveDirection * _moveSpeed;
            var motion = new Vector3(horizontal.x, _verticalVelocity, horizontal.z);
            _characterController.Move(motion * Time.deltaTime);

            if (DesiredMoveDirection.sqrMagnitude > 0.01f)
            {
                var targetRotation = Quaternion.LookRotation(DesiredMoveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
            }
        }
    }
}
