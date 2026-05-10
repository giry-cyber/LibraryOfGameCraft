using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    public abstract class AINode : ScriptableObject
    {
        [SerializeField] private Vector2 _position;
        [SerializeField] private AITransition[] _transitions = System.Array.Empty<AITransition>();

        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        public IReadOnlyList<AITransition> Transitions => _transitions;

        public abstract void OnEnter(AIController context);
        public abstract void Tick(AIController context);
        public abstract void OnExit(AIController context);

        // エディタからのみ呼ぶ変更ヘルパー
        public void AddEmptyTransition()
        {
            var list = new List<AITransition>(_transitions) { new AITransition() };
            _transitions = list.ToArray();
        }

        public void RemoveTransitionAt(int index)
        {
            var list = new List<AITransition>(_transitions);
            if (index >= 0 && index < list.Count)
                list.RemoveAt(index);
            _transitions = list.ToArray();
        }

        public void SetTransitionNextNode(int index, AINode node)
        {
            if (index >= 0 && index < _transitions.Length)
                _transitions[index].SetNextNode(node);
        }

        public void SetTransitionCondition(int index, AICondition condition)
        {
            if (index >= 0 && index < _transitions.Length)
                _transitions[index].SetCondition(condition);
        }
    }
}
