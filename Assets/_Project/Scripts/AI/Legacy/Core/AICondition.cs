using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    public abstract class AICondition : ScriptableObject
    {
        public abstract bool Evaluate(AIController context);
    }
}
