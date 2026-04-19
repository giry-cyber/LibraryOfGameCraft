using UnityEngine;

namespace LibraryOfGamecraft.Dialogue
{
    public class DialogueConditionEvaluator
    {
        private readonly DialogueFlagService _flagService;
        private readonly DialogueHistoryService _historyService;

        public DialogueConditionEvaluator(DialogueFlagService flagService, DialogueHistoryService historyService)
        {
            _flagService = flagService;
            _historyService = historyService;
        }

        // 全条件がtrueならtrue（AND）
        public bool EvaluateAll(DialogueCondition[] conditions)
        {
            if (conditions == null || conditions.Length == 0) return true;
            foreach (var c in conditions)
            {
                if (!Evaluate(c)) return false;
            }
            return true;
        }

        public bool Evaluate(DialogueCondition condition)
        {
            if (condition == null) return true;

            switch (condition.TargetType)
            {
                case ConditionTargetType.FlagBool:
                    return CompareBool(_flagService.GetBool(condition.TargetId), condition.Operator, condition.Value);

                case ConditionTargetType.FlagInt:
                    return CompareInt(_flagService.GetInt(condition.TargetId), condition.Operator, condition.Value);

                case ConditionTargetType.DialogueRead:
                    return CompareBool(_historyService.IsRead(condition.TargetId), condition.Operator, condition.Value);

                default:
                    // Quest/Item は外部システム連携が必要なため、未実装は true を返す
                    Debug.LogWarning($"[DialogueConditionEvaluator] 未対応の条件タイプ: {condition.TargetType}");
                    return true;
            }
        }

        private bool CompareBool(bool actual, ConditionOperator op, string valueStr)
        {
            bool.TryParse(valueStr, out bool expected);
            return op == ConditionOperator.Equal ? actual == expected : actual != expected;
        }

        private bool CompareInt(int actual, ConditionOperator op, string valueStr)
        {
            if (!int.TryParse(valueStr, out int expected))
            {
                Debug.LogWarning($"[DialogueConditionEvaluator] Int値のパース失敗: {valueStr}");
                return false;
            }
            return op switch
            {
                ConditionOperator.Equal        => actual == expected,
                ConditionOperator.NotEqual     => actual != expected,
                ConditionOperator.Greater      => actual > expected,
                ConditionOperator.GreaterOrEqual => actual >= expected,
                ConditionOperator.Less         => actual < expected,
                ConditionOperator.LessOrEqual  => actual <= expected,
                _                              => false
            };
        }
    }
}
