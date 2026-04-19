using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Dialogue
{
    public class DialogueDataRepository
    {
        private readonly Dictionary<string, DialogueSet> _setRegistry =
            new Dictionary<string, DialogueSet>();

        public void Register(DialogueSet set)
        {
            if (set == null) return;
            _setRegistry[set.DialogueSetId] = set;
        }

        public void Unregister(string setId) => _setRegistry.Remove(setId);

        public DialogueSet GetSet(string setId) =>
            _setRegistry.TryGetValue(setId, out var s) ? s : null;

        // 候補セットから条件一致する最優先のものを解決する
        public DialogueSet Resolve(List<DialogueSet> candidates, DialogueConditionEvaluator evaluator)
        {
            if (candidates == null || candidates.Count == 0) return null;

            candidates.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            foreach (var set in candidates)
            {
                if (evaluator.EvaluateAll(set.StartConditions))
                    return set;
            }
            return null;
        }

        public DialogueNodeBase GetNode(DialogueSet set, string nodeId)
        {
            if (set == null || string.IsNullOrEmpty(nodeId)) return null;
            foreach (var node in set.Nodes)
            {
                if (node != null && node.NodeId == nodeId) return node;
            }
            Debug.LogWarning($"[DialogueDataRepository] ノード未発見: {nodeId} (セット: {set.DialogueSetId})");
            return null;
        }
    }
}
