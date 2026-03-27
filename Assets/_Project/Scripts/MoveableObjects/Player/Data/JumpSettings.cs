using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    [CreateAssetMenu(fileName = "JumpSettings", menuName = "LibraryOfGamecraft/Player/JumpSettings")]
    public class JumpSettings : ScriptableObject
    {
        public float JumpForce = 8f;
        // 仕様 23.4: Landing終了の最小待機時間
        public float MinLandingTime = 0.1f;
    }
}
