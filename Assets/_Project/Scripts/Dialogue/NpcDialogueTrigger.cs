using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LibraryOfGamecraft.Dialogue
{
    /// <summary>
    /// NPCに付与するトリガーコンポーネント。
    /// 入力処理は行わず、DialogueInteractController に候補として登録するだけ。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NpcDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private List<DialogueSet> _dialogueSets;
        [SerializeField] private string _playerTag = "Player";

        [Header("頭上プロンプト")]
        [SerializeField] private GameObject _interactPrompt;

        public List<DialogueSet> DialogueSets => _dialogueSets;

        private void Awake()
        {
            if (_interactPrompt != null)
                _interactPrompt.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_playerTag)) return;
            var controller = other.GetComponentInParent<DialogueInteractController>();
            controller?.RegisterCandidate(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(_playerTag)) return;
            var controller = other.GetComponentInParent<DialogueInteractController>();
            controller?.UnregisterCandidate(this);
            ShowPrompt(false);
        }

        public void ShowPrompt(bool show)
        {
            if (_interactPrompt != null)
                _interactPrompt.SetActive(show);
        }

        public void StartDialogue()
        {
            DialogueManager.Instance?.StartDialogue(_dialogueSets);
        }
    }
}
