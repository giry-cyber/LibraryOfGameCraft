using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    // 攻撃距離内でターゲットへ向かって攻撃を発火する。
    // ダメージ処理は AIController.OnAttackTriggered を購読した外部システムが担う。
    [CreateAssetMenu(fileName = "AttackNode", menuName = "LibraryOfGamecraft/AI/Nodes/AttackNode")]
    public class AttackNode : AINode
    {
        private const string KeyNextAttack = "attack_nextAttack";

        [SerializeField] private float _attackInterval  = 1.5f;
        [SerializeField] private float _rotationSpeed   = 10f;

        public override void OnEnter(AIController context)
        {
            context.StopMovement();
            context.Blackboard.Set(KeyNextAttack, 0f);
            Debug.Log($"[AttackNode] {context.SelfTransform.name}: 攻撃開始");
        }

        public override void Tick(AIController context)
        {
            if (context.TargetTransform == null) return;

            context.FaceToward(context.TargetTransform.position, _rotationSpeed);

            var now = Time.time;
            if (now >= context.Blackboard.Get<float>(KeyNextAttack))
            {
                context.Blackboard.Set(KeyNextAttack, now + _attackInterval);
                context.TriggerAttack();
                Debug.Log($"[AttackNode] {context.SelfTransform.name}: 攻撃！");
            }
        }

        public override void OnExit(AIController context) { }
    }
}
