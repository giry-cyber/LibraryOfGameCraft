using UnityEngine;
using LibraryOfGamecraft.Noise;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// プロシージャル地形生成のパラメータ設定。
    /// </summary>
    [CreateAssetMenu(
        fileName = "TerrainSettings",
        menuName = "LibraryOfGamecraft/Terrain/TerrainSettings")]
    public class TerrainSettings : ScriptableObject
    {
        [Header("ワールド基本設定")]
        [Tooltip("乱数シード。同じシードで同じ地形が再現される。")]
        public int worldSeed = 42;

        [Tooltip("海面高さ (ブロック単位)")]
        [Range(1, ChunkData.Height - 1)]
        public int seaLevel = 48;

        [Tooltip("地形の基準高さ")]
        [Range(1, ChunkData.Height - 1)]
        public int baseHeight = 52;

        [Tooltip("高さ変動の最大振れ幅 (ブロック)")]
        [Range(0, 64)]
        public int heightAmplitude = 28;

        [Tooltip("山岳ノイズの振れ幅 (ブロック)")]
        [Range(0, 64)]
        public int mountainAmplitude = 32;

        [Header("地表ノイズ (fBm)")]
        [Tooltip("地表高さを決める NoiseSettings。null のときデフォルト値を使用。")]
        public NoiseSettings surfaceNoise;

        [Tooltip("サンプリング周波数スケール")]
        [Min(0.0001f)]
        public float surfaceFrequency = 0.004f;

        [Header("山岳ノイズ (Ridged)")]
        public NoiseSettings mountainNoise;
        public float mountainFrequency = 0.002f;

        [Header("洞窟ノイズ (3D)")]
        public NoiseSettings caveNoise;
        public float caveFrequency = 0.05f;

        [Tooltip("この値より大きい箇所を洞窟として空洞化する (0‥1)")]
        [Range(0f, 1f)]
        public float caveThreshold = 0.72f;

        [Tooltip("洞窟が生成される最大高さ")]
        [Range(0, ChunkData.Height - 1)]
        public int caveMaxHeight = 50;

        [Header("バイオーム")]
        [Tooltip("気温ノイズのスケール。低いほど広いバイオーム。")]
        public float biomeFrequency = 0.0015f;

        [Tooltip("この気温以上は砂漠バイオーム")]
        [Range(0f, 1f)]
        public float desertThreshold = 0.65f;

        [Tooltip("この気温以下 + 高地 → 雪バイオーム")]
        [Range(0f, 1f)]
        public float snowThreshold = 0.28f;
    }
}
