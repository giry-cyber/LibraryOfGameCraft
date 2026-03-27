using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    [CreateAssetMenu(fileName = "GravitySettings", menuName = "LibraryOfGamecraft/Player/GravitySettings")]
    public class GravitySettings : ScriptableObject
    {
        public float GravityScale = 2f;
        public float MaxFallSpeed = 20f;
        // 落下中に重力をさらに強めるための倍率
        public float FallMultiplier = 1.5f;
    }
}
