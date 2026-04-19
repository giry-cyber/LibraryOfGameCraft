using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Dialogue
{
    public class DialogueEventExecutor
    {
        private readonly Dictionary<string, IDialogueEventHandler> _handlers =
            new Dictionary<string, IDialogueEventHandler>();

        public void RegisterHandler(string eventId, IDialogueEventHandler handler) =>
            _handlers[eventId] = handler;

        public void UnregisterHandler(string eventId) =>
            _handlers.Remove(eventId);

        public IEnumerator ExecuteEvent(DialogueEvent evt, bool isSkipping)
        {
            if (evt == null) yield break;

            if (!_handlers.TryGetValue(evt.EventId, out var handler))
            {
                Debug.LogWarning($"[DialogueEventExecutor] ハンドラー未登録: {evt.EventId}");
                yield break;
            }

            if (isSkipping)
            {
                // スキップ時はポリシーに従って実行判断
                if (evt.SkipPolicy == SkipEventPolicy.MustRunOnSkip)
                    handler.ApplyImmediate(evt); // 即時実行（状態変更のみ）
                // OptionalOnSkip / VisualOnlyOnSkip はスキップ
                yield break;
            }

            if (evt.WaitMode == EventWaitMode.Immediate)
            {
                handler.ApplyImmediate(evt);
            }
            else
            {
                yield return handler.Execute(evt);
            }
        }

        public IEnumerator ExecuteEvents(DialogueEvent[] events, bool isSkipping)
        {
            if (events == null) yield break;
            foreach (var evt in events)
                yield return ExecuteEvent(evt, isSkipping);
        }
    }
}
