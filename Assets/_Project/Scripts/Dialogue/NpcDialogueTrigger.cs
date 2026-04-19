using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LibraryOfGamecraft.Dialogue
{
    /// <summary>
    /// NPCに付与するトリガーコンポーネント。
    /// プレイヤーが範囲内でZキー（決定）を押すと会話を開始する。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NpcDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private List<DialogueSet> _dialogueSets;
        [SerializeField] private string _playerTag = "Player";

        private bool _playerInRange;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(_playerTag))
                _playerInRange = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_playerTag))
                _playerInRange = false;
        }

        private void Update()
        {
            if (!_playerInRange) return;

            var manager = DialogueManager.Instance;
            if (manager == null || manager.IsInDialogue) return;

            var kb = Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                manager.StartDialogue(_dialogueSets);
        }
    }
}
