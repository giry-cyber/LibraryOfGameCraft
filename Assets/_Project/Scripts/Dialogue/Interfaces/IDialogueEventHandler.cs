using System.Collections;

namespace LibraryOfGamecraft.Dialogue
{
    public interface IDialogueEventHandler
    {
        // 通常進行時（アニメーション等を伴う完全な実行）
        IEnumerator Execute(DialogueEvent evt);

        // スキップ時（ゲーム状態のみ即時反映。視覚演出なし）
        void ApplyImmediate(DialogueEvent evt);
    }
}
