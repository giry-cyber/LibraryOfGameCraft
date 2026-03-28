using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    [CreateAssetMenu(fileName = "MovementTuning", menuName = "LibraryOfGamecraft/Player/MovementTuning")]
    public class MovementTuning : ScriptableObject
    {
        [Header("移動速度")]
        public float MoveSpeed = 5f;
        public float Acceleration = 20f;
        public float Deceleration = 20f;

        [Header("斜面")]
        public float MaxSlopeAngle = 45f;
        public float SlideAngle = 55f;

        [Header("段差")]
        public float StepHeight = 0.4f;

        [Header("接地判定")]
        public float GroundCheckRadius = 0.25f;
        public float GroundCheckDistance = 0.15f;
        public float GroundedThreshold = 0.05f;
        public float MinGroundDotProduct = 0.5f;

        [Header("Ground Snap")]
        public float GroundSnapDistance = 0.5f;

        [Header("壁滑り")]
        public bool EnableWallSliding = true;
    }
}
