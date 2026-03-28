using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    public struct InputSnapshot
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool JumpPressed;
        public bool AttackPressed;
        public bool DashPressed;
        public bool InteractPressed;
        public int Frame;
        public float Time;
    }
}
