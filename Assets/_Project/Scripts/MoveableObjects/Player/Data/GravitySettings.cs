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
        // 接地中に与える下向きの押し付け速度。
        // CharacterController が地面との接触を維持するために必要な最小値。
        // GroundSnap の動作条件（VerticalVelocity <= 0）を満たし続けるためにも使う。
        public float GroundStickSpeed = 2f;
    }
}
