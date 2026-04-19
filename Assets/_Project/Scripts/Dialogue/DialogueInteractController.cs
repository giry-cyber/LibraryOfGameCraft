using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LibraryOfGamecraft.Dialogue
{
    /// <summary>
    /// プレイヤーに付与するコンポーネント。
    /// 範囲内の NpcDialogueTrigger 候補を管理し、最も近い1体にだけ話しかける。
    /// </summary>
    public class DialogueInteractController : MonoBehaviour
    {
        private readonly List<NpcDialogueTrigger> _candidates = new List<NpcDialogueTrigger>();
        private NpcDialogueTrigger _nearest;
        private NpcDialogueTrigger _prevNearest;

        private void Update()
        {
            RefreshNearest();
            UpdatePrompts();

            if (_nearest == null) return;
            if (DialogueManager.Instance == null || DialogueManager.Instance.IsInDialogue) return;

            var kb = Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                _nearest.StartDialogue();
        }

        public void RegisterCandidate(NpcDialogueTrigger trigger)
        {
            if (!_candidates.Contains(trigger))
                _candidates.Add(trigger);
        }

        public void UnregisterCandidate(NpcDialogueTrigger trigger)
        {
            _candidates.Remove(trigger);
            if (_nearest == trigger)
                _nearest = null;
        }

        private void RefreshNearest()
        {
            _candidates.RemoveAll(c => c == null);

            if (_candidates.Count == 0)
            {
                _nearest = null;
                return;
            }

            NpcDialogueTrigger nearest = null;
            float minDist = float.MaxValue;
            foreach (var candidate in _candidates)
            {
                float dist = Vector3.SqrMagnitude(candidate.transform.position - transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = candidate;
                }
            }
            _nearest = nearest;
        }

        // 最近傍NPCのプロンプトだけ表示し、それ以外は非表示にする
        private void UpdatePrompts()
        {
            if (_nearest == _prevNearest) return;

            if (_prevNearest != null)
                _prevNearest.ShowPrompt(false);

            bool canInteract = DialogueManager.Instance == null || !DialogueManager.Instance.IsInDialogue;
            if (_nearest != null)
                _nearest.ShowPrompt(canInteract);

            _prevNearest = _nearest;
        }
    }
}
