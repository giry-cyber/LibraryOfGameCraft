using UnityEngine;

namespace LibraryOfGamecraft.BT.Test
{
    // Phase1 動作確認用。以下のツリーをコードで組み立てて BTRunner に渡す。
    //
    // Selector
    //   ├── Sequence          ← Failure で止まるはず（AlwaysFailure があるため）
    //   │     ├── AlwaysSuccess
    //   │     └── AlwaysFailure
    //   └── Sequence          ← こちらが選ばれ、Countdown が 3 フレーム後に Success
    //         ├── AlwaysSuccess
    //         └── Countdown(3frames)
    //
    // 期待ログ:
    //   フレーム1-3: "[CountdownNode] ... 1(2,3)/3 → Running"
    //   フレーム3:   "[CountdownNode] ... 3フレーム経過 → Success"
    //   フレーム4以降: 再び Countdown がリセットされて繰り返す
    [RequireComponent(typeof(BTRunner))]
    public class BTTestRunner : MonoBehaviour
    {
        private void Awake()
        {
            var tree = BTComposite.Create<BTSelector>(
                BTComposite.Create<BTSequence>(
                    AlwaysSuccessNode.Create(),
                    AlwaysFailureNode.Create()   // ここで Sequence が Failure
                ),
                BTComposite.Create<BTSequence>(
                    AlwaysSuccessNode.Create(),
                    CountdownNode.Create(3)      // 3 フレーム後に Success
                )
            );

            GetComponent<BTRunner>().SetRoot(tree);
        }
    }
}
