using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    public abstract class BTNode : ScriptableObject
    {
        public abstract BTStatus Tick(BTContext context);
    }
}
