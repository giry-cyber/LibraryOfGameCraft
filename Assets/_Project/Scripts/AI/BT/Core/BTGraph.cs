using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    [CreateAssetMenu(fileName = "BTGraph", menuName = "LibraryOfGamecraft/BT/BTGraph")]
    public class BTGraph : ScriptableObject
    {
        [SerializeField] private BTNode _rootNode;
        public BTNode RootNode => _rootNode;
    }
}
