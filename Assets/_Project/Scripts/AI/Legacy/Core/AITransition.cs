using System;
using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [Serializable]
    public class AITransition
    {
        [SerializeField] private AICondition _condition;
        [SerializeField] private AINode _nextNode;

        public AICondition Condition => _condition;
        public AINode NextNode => _nextNode;

        // エディタからのみ呼ぶ変更ヘルパー
        public void SetNextNode(AINode node) => _nextNode = node;
        public void SetCondition(AICondition condition) => _condition = condition;
    }
}
