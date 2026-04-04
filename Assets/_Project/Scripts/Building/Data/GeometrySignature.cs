using System;
using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    [Serializable]
    public struct GeometrySignature : IEquatable<GeometrySignature>
    {
        private const float CenterStep = 0.05f;
        private const float NormalStep = 0.1f;
        private const float AreaStep = 0.05f;
        private const float BoundsSizeStep = 0.05f;

        public Vector3Int quantizedCenter;
        public Vector3Int quantizedNormal;
        public int quantizedArea;
        public Vector3Int quantizedBoundsSize;

        public static GeometrySignature Compute(Vector3 center, Vector3 normal, float area, Vector3 boundsSize)
        {
            return new GeometrySignature
            {
                quantizedCenter   = Quantize(center, CenterStep),
                quantizedNormal   = Quantize(normal, NormalStep),
                quantizedArea     = Mathf.RoundToInt(area / AreaStep),
                quantizedBoundsSize = Quantize(boundsSize, BoundsSizeStep)
            };
        }

        private static Vector3Int Quantize(Vector3 v, float step)
        {
            return new Vector3Int(
                Mathf.RoundToInt(v.x / step),
                Mathf.RoundToInt(v.y / step),
                Mathf.RoundToInt(v.z / step)
            );
        }

        // Phase A: center + normal + area の完全一致
        public bool ExactMatchPrimary(GeometrySignature other)
        {
            return quantizedCenter == other.quantizedCenter
                && quantizedNormal == other.quantizedNormal
                && quantizedArea   == other.quantizedArea;
        }

        // Phase B: center + normal + area で ±1 bin を許容
        public bool NearbyMatchPrimary(GeometrySignature other, int binRange = 1)
        {
            return IsNear(quantizedCenter, other.quantizedCenter, binRange)
                && IsNear(quantizedNormal, other.quantizedNormal, binRange)
                && Mathf.Abs(quantizedArea - other.quantizedArea) <= binRange;
        }

        // boundsSize は後フィルタ用 (±1 bin)
        public bool BoundsSizeNearby(GeometrySignature other, int binRange = 1)
        {
            return IsNear(quantizedBoundsSize, other.quantizedBoundsSize, binRange);
        }

        private static bool IsNear(Vector3Int a, Vector3Int b, int range)
        {
            return Mathf.Abs(a.x - b.x) <= range
                && Mathf.Abs(a.y - b.y) <= range
                && Mathf.Abs(a.z - b.z) <= range;
        }

        public bool Equals(GeometrySignature other)
        {
            return quantizedCenter == other.quantizedCenter
                && quantizedNormal == other.quantizedNormal
                && quantizedArea   == other.quantizedArea
                && quantizedBoundsSize == other.quantizedBoundsSize;
        }

        public override bool Equals(object obj) => obj is GeometrySignature other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(quantizedCenter, quantizedNormal, quantizedArea, quantizedBoundsSize);
        }
    }
}
