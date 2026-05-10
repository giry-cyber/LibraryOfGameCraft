using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    public abstract class AINode : ScriptableObject
    {
        [SerializeField] private AITransition[] _transitions = System.Array.Empty<AITransition>();

        public IReadOnlyList<AITransition> Transitions => _transitions;

        public abstract void OnEnter(AIController context);
        public abstract void Tick(AIController context);
        public abstract void OnExit(AIController context);
    }
}
