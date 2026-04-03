using System;
using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Terrain
{
    [CreateAssetMenu(menuName = "LibraryOfGamecraft/Terrain/Generation Profile")]
    public class TerrainGenerationProfile : ScriptableObject
    {
        public int seed = 42;
        public float tileSizeMeters = 500f;
        public int heightmapResolution = 513;
        public float heightScale = 100f;
        public float noiseScale = 0.003f;
        public int octaves = 6;
        [Range(0f, 1f)] public float persistence = 0.5f;
        public float lacunarity = 2f;
        public bool useDomainWarp = false;
        public float domainWarpStrength = 30f;
        public float domainWarpScale = 0.002f;
        public List<TreePrototypeRule> treeRules = new List<TreePrototypeRule>();
    }

    [Serializable]
    public class TreePrototypeRule
    {
        public GameObject prefab;
        public float bendFactor;
        public float minHeight;
        public float maxHeight;
        public float minSlopeDeg = 0f;
        public float maxSlopeDeg = 30f;
        public float densityPer100m2 = 1f;
        public float randomScaleMin = 0.8f;
        public float randomScaleMax = 1.2f;
        public bool randomRotationY = true;
        public float colorJitter = 0.1f;
        public float heightScaleJitter = 0.1f;
    }
}
