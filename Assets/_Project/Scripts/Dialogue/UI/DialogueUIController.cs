using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfGamecraft.Dialogue
{
    /// <summary>
    /// 会話UIの表示・非表示・更新を担当する。
    /// Canvas上の各UI要素をInspectorから紐付けて使用する。
    /// </summary>
    public class DialogueUIController : MonoBehaviour
    {
        [Header("会話ウィンドウ")]
        [SerializeField] private GameObject _dialogueWindow;
        [SerializeField] private TextMeshProUGUI _speakerNameText;
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private GameObject _nextIndicator;

        [Header("オート・スキップ表示")]
        [SerializeField] private GameObject _autoIndicator;
        [SerializeField] private GameObject _skipIndicator;

        [Header("選択肢")]
        [SerializeField] private GameObject _choicePanel;
        [SerializeField] private Transform _choiceContainer;
        [SerializeField] private GameObject _choiceButtonPrefab;

        [Header("ログ")]
        [SerializeField] private GameObject _logPanel;
        [SerializeField] private Transform _logContainer;
        [SerializeField] private GameObject _logEntryPrefab;

        private readonly List<GameObject> _choiceButtons = new List<GameObject>();

        private void Awake()
        {
            // Unity の UnityEngine.Object は ?. 演算子が機能しないため明示的に null チェックする
            SetActiveIfNotNull(_dialogueWindow, false);
            SetActiveIfNotNull(_choicePanel, false);
            SetActiveIfNotNull(_logPanel, false);
            SetActiveIfNotNull(_nextIndicator, false);
            SetActiveIfNotNull(_autoIndicator, false);
            SetActiveIfNotNull(_skipIndicator, false);
        }

        public void Show()
        {
            SetActiveIfNotNull(_dialogueWindow, true);
        }

        public void Hide()
        {
            SetActiveIfNotNull(_dialogueWindow, false);
            SetActiveIfNotNull(_choicePanel, false);
        }

        public void SetSpeakerName(string name)
        {
            if (_speakerNameText != null)
                _speakerNameText.text = name ?? string.Empty;
        }

        public void SetTextPartial(string text)
        {
            if (_dialogueText != null)
                _dialogueText.text = text;
        }

        public void SetTextInstant(string text)
        {
            if (_dialogueText != null)
                _dialogueText.text = text;
        }

        public void ShowNextIndicator(bool show)
        {
            SetActiveIfNotNull(_nextIndicator, show);
        }

        public void SetAutoIndicator(bool active)
        {
            SetActiveIfNotNull(_autoIndicator, active);
        }

        public void SetSkipIndicator(bool active)
        {
            SetActiveIfNotNull(_skipIndicator, active);
        }

        // ─────────────────────────────────────
        // 選択肢
        // ─────────────────────────────────────

        public void ShowChoices(ChoiceItemState[] choices, string promptText)
        {
            ClearChoiceButtons();
            SetActiveIfNotNull(_choicePanel, true);

            if (_choiceButtonPrefab == null || _choiceContainer == null) return;

            foreach (var state in choices)
            {
                if (!state.Visible) continue;

                var go = Instantiate(_choiceButtonPrefab, _choiceContainer);
                _choiceButtons.Add(go);

                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = state.Choice.ChoiceText;

                var button = go.GetComponent<Button>();
                if (button != null)
                    button.interactable = state.Enabled;

                var graphic = go.GetComponent<Graphic>();
                if (graphic != null)
                    graphic.color = state.Enabled ? Color.white : Color.gray;
            }
        }

        public void HideChoices()
        {
            ClearChoiceButtons();
            SetActiveIfNotNull(_choicePanel, false);
        }

        public void SetChoiceCursor(int index)
        {
            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                var graphic = _choiceButtons[i].GetComponent<Graphic>();
                if (graphic != null)
                    graphic.color = (i == index) ? Color.yellow : Color.white;
            }
        }

        private void ClearChoiceButtons()
        {
            foreach (var go in _choiceButtons)
                Destroy(go);
            _choiceButtons.Clear();
        }

        // ─────────────────────────────────────
        // 会話ログ
        // ─────────────────────────────────────

        public void ShowLog(IReadOnlyList<DialogueLogEntry> entries)
        {
            if (_logPanel == null) return;

            ClearLogEntries();
            _logPanel.SetActive(true);

            if (_logEntryPrefab == null || _logContainer == null) return;

            foreach (var entry in entries)
            {
                var go = Instantiate(_logEntryPrefab, _logContainer);
                var texts = go.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = entry.SpeakerName;
                    texts[1].text = entry.Text;
                }
                else if (texts.Length == 1)
                {
                    texts[0].text = $"{entry.SpeakerName}: {entry.Text}";
                }
            }
        }

        public void HideLog()
        {
            ClearLogEntries();
            if (_logPanel != null) _logPanel.SetActive(false);
        }

        private void ClearLogEntries()
        {
            if (_logContainer == null) return;
            foreach (Transform child in _logContainer)
                Destroy(child.gameObject);
        }

        private static void SetActiveIfNotNull(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
