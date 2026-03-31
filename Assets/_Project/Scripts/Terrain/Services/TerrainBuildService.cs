using UnityEngine;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// Phase 1 ハイトマップ生成フローをオーケストレーションする。
    /// ノイズ生成 → ハイトマップ生成 → Terrain 適用 → バイナリ保存 を一括実行する。
    /// </summary>
    public class TerrainBuildService
    {
        public void Build(
            UnityEngine.Terrain terrain,
            TerrainGenerationProfile profile,
            TerrainPersistentData persistentData,
            Vector2 tileOrigin = default)
        {
            // ノイズ・ドメインワープのファクトリを生成
            var noiseFactory = new FractalNoise2DFactory(
                profile.noiseScale,
                profile.octaves,
                profile.persistence,
                profile.lacunarity);

            IDomainWarp2D domainWarp = null;
            if (profile.useDomainWarp)
            {
                var warpFactory = new SimplexDomainWarp2DFactory(
                    profile.domainWarpScale,
                    profile.domainWarpStrength);
                domainWarp = warpFactory.Create(profile.seed);
            }

            INoise2D noise = noiseFactory.Create(profile.seed);
            var generator = new TerrainGenerator(noise, domainWarp);

            // ハイトマップ生成
            float[] generated = generator.Generate(profile, tileOrigin);

            // 手動デルタの読み込み
            float[] manualDelta = null;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(persistentData.manualDeltaPath))
                manualDelta = HeightMapIO.Load(persistentData.manualDeltaPath);
#endif

            // Terrain に適用
            TerrainApplier.Apply(
                terrain,
                generated,
                manualDelta,
                profile.heightmapResolution,
                profile.tileSizeMeters,
                profile.heightScale);

#if UNITY_EDITOR
            // 生成ハイトマップを保存
            if (!string.IsNullOrEmpty(persistentData.generatedHeightPath))
                HeightMapIO.Save(generated, persistentData.generatedHeightPath);

            // 手動デルタが存在しない場合はゼロ配列を初期ファイルとして保存
            if (manualDelta == null && !string.IsNullOrEmpty(persistentData.manualDeltaPath))
            {
                float[] zeroArray = new float[profile.heightmapResolution * profile.heightmapResolution];
                HeightMapIO.Save(zeroArray, persistentData.manualDeltaPath);
            }

            UnityEditor.EditorUtility.SetDirty(persistentData);
#endif
        }
    }
}
