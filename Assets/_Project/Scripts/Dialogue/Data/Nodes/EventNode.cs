using System;
using LibraryOfGamecraft.Events;

namespace LibraryOfGamecraft.Dialogue
{
    [Serializable]
    public class EventNode : DialogueNodeBase
    {
        // 文字列IDベースのイベント（IDialogueEventHandler と組み合わせて使用）
        public DialogueEvent[] Events;
        public EventWaitMode WaitMode;

        // ScriptableObject チャンネルベースのイベント（GameEventListener で受け取る）
        public GameEvent[] GameEvents;
    }
}
