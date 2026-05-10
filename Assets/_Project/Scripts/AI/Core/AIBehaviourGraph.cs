using UnityEngine;

namespace LibraryOfGamecraft.AI
{
    [CreateAssetMenu(fileName = "NewAIBehaviourGraph", menuName = "LibraryOfGamecraft/AI/BehaviourGraph")]
    public class AIBehaviourGraph : ScriptableObject
    {
        [SerializeField] private AINode _startNode;

        public AINode StartNode => _startNode;
    }
}
