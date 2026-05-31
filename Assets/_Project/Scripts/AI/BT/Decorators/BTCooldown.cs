using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // 子が完了した後、_cooldownTime 秒間は _statusOnCooldown を返して実行をブロックする。
    // Running（デフォルト）: 親が「待機中」と見なし他のブランチへフォールスルーしない。攻撃待機に適する。
    // Failure: 親が次の代替ブランチを試みる。複数選択肢のある汎用用途に適する。
    [CreateAssetMenu(fileName = "BTCooldown", menuName = "LibraryOfGamecraft/BT/Decorators/Cooldown")]
    public class BTCooldown : BTDecorator
    {
        [SerializeField] private float     _cooldownTime      = 3f;
        [SerializeField] private BTStatus  _statusOnCooldown  = BTStatus.Running;

        private string ReadyAtKey => $"__cd_{GetInstanceID()}";

        protected override BTStatus Execute(BTContext ctx)
        {
            if (Time.time < ctx.Blackboard.Get<float>(ReadyAtKey, 0f))
                return _statusOnCooldown;

            if (_child == null) return BTStatus.Failure;

            var status = _child.Tick(ctx);

            if (status != BTStatus.Running)
                ctx.Blackboard.Set(ReadyAtKey, Time.time + _cooldownTime);

            return status;
        }
    }
}
