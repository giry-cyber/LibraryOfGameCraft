using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    // AIController.OnNodeEntered を受けて Animator パラメータを自動設定する。
    // ノードごとのマッピングは ScriptableObject ではなくインスペクタで直接設定する（キャラクターに固有のため）。
    [RequireComponent(typeof(AIController))]
    [RequireComponent(typeof(Animator))]
    public class AIAnimatorAdapter : MonoBehaviour
    {
        public enum ParamType { Bool, Int, Float, Trigger }

        [System.Serializable]
        public class NodeMapping
        {
            public AINode    node;
            public string    parameterName;
            public ParamType paramType;
            public bool      boolValue;
            public int       intValue;
            public float     floatValue;
        }

        [SerializeField] private List<NodeMapping> _nodeMappings = new();

        [Header("Speed Parameter（ブレンドツリー用）")]
        [SerializeField] private string _speedParameter = "Speed";    // 空文字で無効
        [SerializeField] private float  _speedDampTime  = 0.1f;

        private AIController _aiController;
        private Animator     _animator;

        private void Awake()
        {
            _aiController = GetComponent<AIController>();
            _animator     = GetComponent<Animator>();
        }

        private void OnEnable()  => _aiController.OnNodeEntered += HandleNodeEntered;
        private void OnDisable() => _aiController.OnNodeEntered -= HandleNodeEntered;

        private void Update()
        {
            if (!string.IsNullOrEmpty(_speedParameter))
                _animator.SetFloat(_speedParameter, _aiController.Velocity.magnitude, _speedDampTime, Time.deltaTime);
        }

        private void HandleNodeEntered(AINode node)
        {
            foreach (var m in _nodeMappings)
            {
                if (m.node != node) continue;
                Apply(m);
                break;
            }
        }

        private void Apply(NodeMapping m)
        {
            switch (m.paramType)
            {
                case ParamType.Bool:    _animator.SetBool   (m.parameterName, m.boolValue);  break;
                case ParamType.Int:     _animator.SetInteger(m.parameterName, m.intValue);   break;
                case ParamType.Float:   _animator.SetFloat  (m.parameterName, m.floatValue); break;
                case ParamType.Trigger: _animator.SetTrigger(m.parameterName);               break;
            }
        }
    }
}
