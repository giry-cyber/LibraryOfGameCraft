using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // 子が完了した後、_cooldownTime 秒間は Failure を返して実行をブロックする。
    // 攻撃間隔・スキルのリキャストなどに使う。
    [CreateAssetMenu(fileName = "BTCooldown", menuName = "LibraryOfGamecraft/BT/Decorators/Cooldown")]
    public class BTCooldown : BTDecorator
    {
        [SerializeField] private float _cooldownTime = 3f;

        private string ReadyAtKey => $"__cd_{GetInstanceID()}";

        protected override BTStatus Execute(BTContext ctx)
        {
            if (Time.time < ctx.Blackboard.Get<float>(ReadyAtKey, 0f))
                return BTStatus.Failure;

            if (_child == null) return BTStatus.Failure;

            var status = _child.Tick(ctx);

            if (status != BTStatus.Running)
                ctx.Blackboard.Set(ReadyAtKey, Time.time + _cooldownTime);

            return status;
        }
    }
}
