using System;
using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Dialogue
{
    public struct ChoiceItemState
    {
        public ChoiceItem Choice;
        public bool Visible;
        public bool Enabled;

        public ChoiceItemState(ChoiceItem choice, bool visible, bool enabled)
        {
            Choice = choice;
            Visible = visible;
            Enabled = enabled;
        }
    }

    // ノード解釈・遷移ロジックを担当する。MonoBehaviour非依存。
    public class DialogueRunner
    {
        private readonly DialogueDataRepository _repository;
        private readonly DialogueConditionEvaluator _conditionEvaluator;

        public DialogueSet CurrentSet { get; private set; }
        public DialogueNodeBase CurrentNode { get; private set; }

        public DialogueRunner(
            DialogueDataRepository repository,
            DialogueConditionEvaluator conditionEvaluator)
        {
            _repository = repository;
            _conditionEvaluator = conditionEvaluator;
        }

        public void SetCurrentSet(DialogueSet set) => CurrentSet = set;

        public void SetCurrentNode(DialogueNodeBase node) => CurrentNode = node;

        public DialogueNodeBase ResolveStartNode()
        {
            if (CurrentSet == null)
            {
                Debug.LogError("[DialogueRunner] CurrentSet が null です。");
                return null;
            }
            return _repository.GetNode(CurrentSet, CurrentSet.StartNodeId);
        }

        public DialogueNodeBase GetNextNode(string nextNodeId)
        {
            if (string.IsNullOrEmpty(nextNodeId)) return null;
            return _repository.GetNode(CurrentSet, nextNodeId);
        }

        // BranchNodeの条件評価を行い、遷移先NodeIdを返す
        public string EvaluateBranch(BranchNode branchNode)
        {
            if (branchNode.Branches == null || branchNode.Branches.Length == 0)
                return branchNode.DefaultNextNodeId;

            var sorted = new List<BranchItem>(branchNode.Branches);
            sorted.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            foreach (var branch in sorted)
            {
                if (_conditionEvaluator.Evaluate(branch.Condition))
                {
                    Debug.Log($"[DialogueRunner] 分岐一致: Priority={branch.Priority} → {branch.NextNodeId}");
                    return branch.NextNodeId;
                }
            }

            Debug.Log($"[DialogueRunner] 分岐デフォルト → {branchNode.DefaultNextNodeId}");
            return branchNode.DefaultNextNodeId;
        }

        // ChoiceNodeの各選択肢の表示・有効状態を解決する
        public ChoiceItemState[] ResolveChoiceStates(ChoiceNode choiceNode)
        {
            if (choiceNode.Choices == null) return Array.Empty<ChoiceItemState>();

            var states = new ChoiceItemState[choiceNode.Choices.Length];
            for (int i = 0; i < choiceNode.Choices.Length; i++)
            {
                var choice = choiceNode.Choices[i];
                bool visible = _conditionEvaluator.EvaluateAll(choice.ShowConditions);
                bool enabled = visible && _conditionEvaluator.EvaluateAll(choice.EnableConditions);
                states[i] = new ChoiceItemState(choice, visible, enabled);
            }
            return states;
        }
    }
}
