using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    // 発見直後の警戒状態。移動を止めてターゲット方向を向く。
    // どれだけ警戒するかは TimerCondition で制御し、次のノード（ChaseNode）へ遷移する。
    [CreateAssetMenu(fileName = "AlertNode", menuName = "LibraryOfGamecraft/AI/Nodes/AlertNode")]
    public class AlertNode : AINode
    {
        [SerializeField] private float _rotationSpeed = 8f;

        public override void OnEnter(AIController context)
        {
            context.StopMovement();
            Debug.Log($"[AlertNode] {context.SelfTransform.name}: 警戒！");
        }

        public override void Tick(AIController context)
        {
            if (context.TargetTransform == null) return;
            context.FaceToward(context.TargetTransform.position, _rotationSpeed);
        }

        public override void OnExit(AIController context) { }
    }
}
