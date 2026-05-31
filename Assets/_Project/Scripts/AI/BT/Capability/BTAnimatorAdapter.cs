using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // BT の実行状態を Animator パラメータに変換するアダプター。
    // BT ノード自身は Animator を知らなくてよい。
    //
    //  CharacterMotor.Velocity.magnitude → Speed (Float)    ← ブレンドツリー
    //  Blackboard["target"] != null      → HasTarget (Bool) ← 戦闘状態
    //  AttackCapability.OnAttackTriggered → Attack (Trigger) ← 攻撃モーション
    [RequireComponent(typeof(BTRunner))]
    [RequireComponent(typeof(Animator))]
    public class BTAnimatorAdapter : MonoBehaviour
    {
        [Header("移動速度（ブレンドツリー）")]
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private float  _speedDamp  = 0.1f;

        [Header("ターゲット検知")]
        [SerializeField] private string _hasTargetParam = "HasTarget";

        [Header("攻撃")]
        [SerializeField] private string _attackTriggerParam = "Attack";

        private BTRunner         _runner;
        private CharacterMotor   _motor;   // optional
        private AttackCapability _attack;  // optional
        private Animator         _animator;

        private void Awake()
        {
            _runner   = GetComponent<BTRunner>();
            _motor    = GetComponent<CharacterMotor>();
            _attack   = GetComponent<AttackCapability>();
            _animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (_attack != null)
                _attack.OnAttackTriggered.AddListener(OnAttackTriggered);
        }

        private void OnDisable()
        {
            if (_attack != null)
                _attack.OnAttackTriggered.RemoveListener(OnAttackTriggered);
        }

        private void Update()
        {
            UpdateSpeed();
            UpdateHasTarget();
        }

        private void UpdateSpeed()
        {
            if (string.IsNullOrEmpty(_speedParam) || _motor == null) return;
            _animator.SetFloat(_speedParam, _motor.Velocity.magnitude, _speedDamp, Time.deltaTime);
        }

        private void UpdateHasTarget()
        {
            if (string.IsNullOrEmpty(_hasTargetParam)) return;
            var bb = _runner.Blackboard;
            if (bb == null) return;
            _animator.SetBool(_hasTargetParam, bb.Get<Transform>(BTKeys.Target) != null);
        }

        private void OnAttackTriggered()
        {
            if (string.IsNullOrEmpty(_attackTriggerParam)) return;
            _animator.SetTrigger(_attackTriggerParam);
        }
    }
}
