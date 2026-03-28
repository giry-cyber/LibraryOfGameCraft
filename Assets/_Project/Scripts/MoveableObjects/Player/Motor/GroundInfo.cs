using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    public struct GroundInfo
    {
        public bool IsGrounded;
        public Vector3 GroundNormal;
        public Vector3 GroundPoint;
        public float GroundDistance;
        public float SlopeAngle;
        public Collider GroundCollider;

        public static GroundInfo Airborne => new GroundInfo
        {
            IsGrounded = false,
            GroundNormal = Vector3.up,
            SlopeAngle = 0f,
        };
    }
}
