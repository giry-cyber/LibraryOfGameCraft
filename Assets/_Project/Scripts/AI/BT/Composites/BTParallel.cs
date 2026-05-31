using System.Linq;
using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    public enum ParallelPolicy
    {
        RequireOne,  // 1つでも条件を満たせば OK
        RequireAll,  // 全てが条件を満たす必要がある
    }

    // 全子を毎フレーム必ず Tick する。成功・失敗はポリシーで制御。
    // 例: SuccessPolicy=RequireAll / FailurePolicy=RequireOne
    //   → 全子が成功すれば Success、1つでも失敗すれば即 Failure（AND 的動作）
    // 例: SuccessPolicy=RequireOne / FailurePolicy=RequireAll
    //   → 1つでも成功すれば Success、全子が失敗すると Failure（OR 的動作）
    [CreateAssetMenu(fileName = "BTParallel", menuName = "LibraryOfGamecraft/BT/Composites/Parallel")]
    public class BTParallel : BTComposite
    {
        [SerializeField] private ParallelPolicy _successPolicy = ParallelPolicy.RequireAll;
        [SerializeField] private ParallelPolicy _failurePolicy = ParallelPolicy.RequireOne;

        protected override BTStatus Execute(BTContext ctx)
        {
            int successCount = 0;
            int failureCount = 0;
            int count       = 0;

            foreach (var child in _children)
            {
                if (child == null) continue;
                count++;
                var status = child.Tick(ctx);
                if      (status == BTStatus.Success) successCount++;
                else if (status == BTStatus.Failure) failureCount++;
            }

            if (count == 0) return BTStatus.Success;

            // Failure 判定を先に評価する
            bool failed = _failurePolicy == ParallelPolicy.RequireOne
                ? failureCount > 0
                : failureCount >= count;
            if (failed) return BTStatus.Failure;

            bool succeeded = _successPolicy == ParallelPolicy.RequireOne
                ? successCount > 0
                : successCount >= count;
            return succeeded ? BTStatus.Success : BTStatus.Running;
        }
    }
}
