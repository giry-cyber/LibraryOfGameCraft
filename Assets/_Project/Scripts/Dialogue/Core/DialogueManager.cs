using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Dialogue
{
    /// <summary>
    /// 会話システム全体の状態管理・進行制御を担うメインクラス。
    /// シーンに1つ配置し、DialogueUIControllerを紐付けて使用する。
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [SerializeField] private DialogueUIController _uiController;
        [SerializeField] private DialogueFlagDatabase _flagDatabase;

        [Header("進行設定")]
        [SerializeField] private float _defaultTextSpeed = 0.03f;
        [SerializeField] private float _defaultAutoDelay = 2f;
        [SerializeField] private float _skipInterval = 0.02f;

        private DialogueFlagService _flagService;
        private DialogueConditionEvaluator _conditionEvaluator;
        private DialogueEventExecutor _eventExecutor;
        private DialogueHistoryService _historyService;
        private DialogueDataRepository _dataRepository;
        private DialogueRunner _runner;

        private DialogueState _state = DialogueState.Idle;
        private bool _isAutoMode;
        private bool _isSkipMode;

        // 入力フラグ（Update → コルーチン間の橋渡し）
        private bool _advancePressed;
        private bool _choiceUpPressed;
        private bool _choiceDownPressed;
        private bool _choiceConfirmPressed;

        // ノード処理内部状態
        private string _nextNodeId;
        private int _selectedChoiceIndex;

        // Public API
        public DialogueState State => _state;
        public bool IsAutoMode => _isAutoMode;
        public bool IsSkipMode => _isSkipMode;
        public bool IsInDialogue => _state != DialogueState.Idle;
        public DialogueFlagService FlagService => _flagService;
        public DialogueEventExecutor EventExecutor => _eventExecutor;
        public DialogueHistoryService HistoryService => _historyService;

        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;
        public event Action<DialogueState> OnStateChanged;

        private void Awake()
        {
            _flagService = new DialogueFlagService();
            _historyService = new DialogueHistoryService();
            _conditionEvaluator = new DialogueConditionEvaluator(_flagService, _historyService);
            _eventExecutor = new DialogueEventExecutor();
            _dataRepository = new DialogueDataRepository();
            _runner = new DialogueRunner(_dataRepository, _conditionEvaluator);

            if (_flagDatabase != null)
                _flagService.Initialize(_flagDatabase);
        }

        private void Update()
        {
            if (_state == DialogueState.Idle) return;

            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                _advancePressed = true;
                _choiceConfirmPressed = true;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                _choiceUpPressed = true;

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                _choiceDownPressed = true;

            if (Input.GetKeyDown(KeyCode.A))
                ToggleAutoMode();

            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
                ToggleSkipMode();
        }

        /// <summary>候補セットのリストから条件に合う会話を開始する。</summary>
        public void StartDialogue(List<DialogueSet> candidates)
        {
            if (_state != DialogueState.Idle)
            {
                Debug.LogWarning("[DialogueManager] 会話中のため開始できません。");
                return;
            }
            _historyService.ClearSessionLog();
            StartCoroutine(RunDialogue(candidates));
        }

        /// <summary>単一セットで会話を開始する。</summary>
        public void StartDialogue(DialogueSet set)
        {
            StartDialogue(new List<DialogueSet> { set });
        }

        public void ToggleAutoMode()
        {
            _isAutoMode = !_isAutoMode;
            _uiController?.SetAutoIndicator(_isAutoMode);
        }

        public void ToggleSkipMode()
        {
            _isSkipMode = !_isSkipMode;
            _uiController?.SetSkipIndicator(_isSkipMode);
        }

        private void SetState(DialogueState newState)
        {
            _state = newState;
            OnStateChanged?.Invoke(newState);
        }

        // ─────────────────────────────────────
        // メイン進行コルーチン
        // ─────────────────────────────────────

        private IEnumerator RunDialogue(List<DialogueSet> candidates)
        {
            SetState(DialogueState.Opening);
            _uiController?.Show();
            OnDialogueStarted?.Invoke();
            yield return null;

            SetState(DialogueState.ResolvingStartNode);
            var set = _dataRepository.Resolve(candidates, _conditionEvaluator);
            if (set == null)
            {
                Debug.LogWarning("[DialogueManager] 条件に合う会話セットが見つかりません。");
                yield return CloseDialogue();
                yield break;
            }

            _runner.SetCurrentSet(set);
            var node = _runner.ResolveStartNode();
            if (node == null)
            {
                Debug.LogError($"[DialogueManager] 開始ノード '{set.StartNodeId}' が見つかりません (セット: {set.DialogueSetId})。");
                yield return CloseDialogue();
                yield break;
            }

            // ノードループ
            int safetyCounter = 0;
            while (node != null)
            {
                if (++safetyCounter > 10000)
                {
                    Debug.LogError("[DialogueManager] 無限ループを検出しました。会話を強制終了します。");
                    break;
                }

                _nextNodeId = null;
                yield return ProcessNode(node);

                if (string.IsNullOrEmpty(_nextNodeId)) break;

                node = _runner.GetNextNode(_nextNodeId);
                if (node == null)
                {
                    Debug.LogError($"[DialogueManager] ノード '{_nextNodeId}' が見つかりません。");
                    break;
                }
            }

            yield return CloseDialogue();
        }

        private IEnumerator ProcessNode(DialogueNodeBase node)
        {
            _runner.SetCurrentNode(node);
            SetState(DialogueState.EnterNode);

            bool isSkipping = ResolveSkipState(node);

            // PreEvents
            if (node.PreEvents != null && node.PreEvents.Length > 0)
            {
                SetState(DialogueState.ExecutingEvent);
                yield return _eventExecutor.ExecuteEvents(node.PreEvents, isSkipping);
            }

            // ノード種別処理
            switch (node.NodeType)
            {
                case DialogueNodeType.Message:
                    yield return ProcessMessageNode((MessageNode)node, isSkipping);
                    break;
                case DialogueNodeType.Choice:
                    yield return ProcessChoiceNode((ChoiceNode)node);
                    break;
                case DialogueNodeType.Branch:
                    yield return ProcessBranchNode((BranchNode)node);
                    break;
                case DialogueNodeType.Event:
                    yield return ProcessEventNode((EventNode)node, isSkipping);
                    break;
                case DialogueNodeType.Sequence:
                    yield return ProcessSequenceNode((SequenceNode)node, isSkipping);
                    break;
                case DialogueNodeType.Jump:
                    ProcessJumpNode((JumpNode)node);
                    break;
                case DialogueNodeType.End:
                    yield return ProcessEndNode((EndNode)node, isSkipping);
                    break;
                default:
                    Debug.LogWarning($"[DialogueManager] 未対応のノード種別: {node.NodeType}");
                    _nextNodeId = node.NextNodeId;
                    break;
            }

            // PostEvents
            if (node.PostEvents != null && node.PostEvents.Length > 0)
            {
                SetState(DialogueState.ExecutingEvent);
                yield return _eventExecutor.ExecuteEvents(node.PostEvents, isSkipping);
            }

            _historyService.MarkRead(node.NodeId);
        }

        // ─────────────────────────────────────
        // ノード種別ごとの処理
        // ─────────────────────────────────────

        private IEnumerator ProcessMessageNode(MessageNode node, bool isSkipping)
        {
            if (node.LogPolicy == LogPolicy.Record)
                _historyService.AddLog(node.SpeakerDisplayName, node.Text);

            _uiController?.SetSpeakerName(node.SpeakerDisplayName);
            _uiController?.ShowNextIndicator(false);

            if (isSkipping)
            {
                SetState(DialogueState.Skipping);
                _uiController?.SetTextInstant(node.Text);
                yield return new WaitForSeconds(_skipInterval);
                _nextNodeId = node.NextNodeId;
                yield break;
            }

            float textSpeed = node.TextSpeedOverride >= 0f ? node.TextSpeedOverride : _defaultTextSpeed;

            // 逐次表示（タイピング）
            SetState(DialogueState.Typing);
            _advancePressed = false;
            bool interrupted = false;

            for (int i = 1; i <= node.Text.Length; i++)
            {
                _uiController?.SetTextPartial(node.Text.Substring(0, i));

                if (_advancePressed)
                {
                    _advancePressed = false;
                    interrupted = true;
                    break;
                }

                yield return new WaitForSeconds(textSpeed);
            }

            if (interrupted)
                _uiController?.SetTextInstant(node.Text);

            // 全文表示後の待機
            SetState(DialogueState.WaitingForAdvance);
            _uiController?.ShowNextIndicator(true);
            _advancePressed = false;

            bool useAuto = node.AutoAdvanceEnabled || _isAutoMode;
            float autoDelay = node.AutoAdvanceDelay > 0f ? node.AutoAdvanceDelay : _defaultAutoDelay;

            if (useAuto)
            {
                SetState(DialogueState.AutoAdvancing);
                float timer = 0f;
                while (timer < autoDelay)
                {
                    if (_advancePressed) { _advancePressed = false; break; }
                    timer += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                while (!_advancePressed)
                    yield return null;
                _advancePressed = false;
            }

            _uiController?.ShowNextIndicator(false);
            _nextNodeId = node.NextNodeId;
        }

        private IEnumerator ProcessChoiceNode(ChoiceNode node)
        {
            // 選択肢では必ずスキップを停止
            if (_isSkipMode)
            {
                _isSkipMode = false;
                _uiController?.SetSkipIndicator(false);
            }

            var choiceStates = _runner.ResolveChoiceStates(node);

            if (choiceStates.Length == 0)
            {
                Debug.LogWarning($"[DialogueManager] 選択肢が0件です (ノード: {node.NodeId})。");
                _nextNodeId = node.NextNodeId;
                yield break;
            }

            SetState(DialogueState.SelectingChoice);
            _uiController?.ShowChoices(choiceStates, node.PromptText);

            // 最初の選択可能な項目にカーソルを合わせる
            _selectedChoiceIndex = FindNextEnabledChoice(choiceStates, 0, 1);
            _uiController?.SetChoiceCursor(_selectedChoiceIndex);

            _choiceUpPressed = false;
            _choiceDownPressed = false;
            _choiceConfirmPressed = false;

            while (true)
            {
                if (_choiceUpPressed)
                {
                    _choiceUpPressed = false;
                    _selectedChoiceIndex = FindNextEnabledChoice(choiceStates, _selectedChoiceIndex, -1);
                    _uiController?.SetChoiceCursor(_selectedChoiceIndex);
                }

                if (_choiceDownPressed)
                {
                    _choiceDownPressed = false;
                    _selectedChoiceIndex = FindNextEnabledChoice(choiceStates, _selectedChoiceIndex, 1);
                    _uiController?.SetChoiceCursor(_selectedChoiceIndex);
                }

                if (_choiceConfirmPressed)
                {
                    _choiceConfirmPressed = false;
                    var selected = choiceStates[_selectedChoiceIndex];
                    if (selected.Enabled)
                    {
                        if (selected.Choice.OnSelectedEvents != null && selected.Choice.OnSelectedEvents.Length > 0)
                        {
                            SetState(DialogueState.ExecutingEvent);
                            yield return _eventExecutor.ExecuteEvents(selected.Choice.OnSelectedEvents, false);
                        }
                        _nextNodeId = selected.Choice.NextNodeId;
                        break;
                    }
                }

                yield return null;
            }

            _uiController?.HideChoices();
        }

        private IEnumerator ProcessBranchNode(BranchNode node)
        {
            SetState(DialogueState.EvaluatingBranch);
            _nextNodeId = _runner.EvaluateBranch(node);
            yield return null;
        }

        private IEnumerator ProcessEventNode(EventNode node, bool isSkipping)
        {
            SetState(DialogueState.ExecutingEvent);
            yield return _eventExecutor.ExecuteEvents(node.Events, isSkipping);
            _nextNodeId = node.NextNodeId;
        }

        private IEnumerator ProcessSequenceNode(SequenceNode node, bool isSkipping)
        {
            SetState(DialogueState.WaitingExternalSequence);
            // TODO: 外部シーケンスシステム（タイムライン等）との連携実装
            // 現時点では即時完了
            yield return null;
            _nextNodeId = node.NextNodeId;
        }

        private void ProcessJumpNode(JumpNode node)
        {
            if (!string.IsNullOrEmpty(node.TargetSetId))
            {
                var targetSet = _dataRepository.GetSet(node.TargetSetId);
                if (targetSet != null)
                    _runner.SetCurrentSet(targetSet);
                else
                    Debug.LogError($"[DialogueManager] ジャンプ先セット未発見: {node.TargetSetId}");
            }
            _nextNodeId = node.TargetNodeId;
        }

        private IEnumerator ProcessEndNode(EndNode node, bool isSkipping)
        {
            if (node.EndEvents != null && node.EndEvents.Length > 0)
            {
                SetState(DialogueState.ExecutingEvent);
                yield return _eventExecutor.ExecuteEvents(node.EndEvents, isSkipping);
            }
            _nextNodeId = null;
        }

        private IEnumerator CloseDialogue()
        {
            SetState(DialogueState.Closing);
            _uiController?.Hide();
            _isAutoMode = false;
            _isSkipMode = false;
            _advancePressed = false;
            _choiceConfirmPressed = false;
            yield return null;
            SetState(DialogueState.Idle);
            OnDialogueEnded?.Invoke();
        }

        // ─────────────────────────────────────
        // ユーティリティ
        // ─────────────────────────────────────

        // スキップ状態を確認し、ノードのポリシーに基づいて解除または継続を決める
        private bool ResolveSkipState(DialogueNodeBase node)
        {
            if (!_isSkipMode) return false;

            var policy = node.SkipPolicy == SkipPolicy.Inherit
                ? (_runner.CurrentSet?.DefaultSkipPolicy ?? SkipPolicy.Allowed)
                : node.SkipPolicy;

            if (policy == SkipPolicy.Disallowed)
            {
                _isSkipMode = false;
                _uiController?.SetSkipIndicator(false);
                return false;
            }

            // 既読スキップ: 未読ノードでスキップ停止
            if (!_historyService.IsRead(node.NodeId))
            {
                _isSkipMode = false;
                _uiController?.SetSkipIndicator(false);
                return false;
            }

            return true;
        }

        private int FindNextEnabledChoice(ChoiceItemState[] states, int current, int direction)
        {
            int next = current;
            for (int i = 0; i < states.Length; i++)
            {
                next = (next + direction + states.Length) % states.Length;
                if (states[next].Visible && states[next].Enabled)
                    return next;
            }
            return current; // 全て無効なら現在位置を維持
        }
    }
}
