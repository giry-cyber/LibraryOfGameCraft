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

            // 手動デルタの読み込み・Unity 標準ツール編集の吸収
            float[] manualDelta = null;
#if UNITY_EDITOR
            bool isFirstGenerate = string.IsNullOrEmpty(persistentData.generatedHeightPath)
                                || !System.IO.File.Exists(
                                       System.IO.Path.GetFullPath(persistentData.generatedHeightPath));

            if (isFirstGenerate)
            {
                // 初回: manualDelta がなければゼロ配列
                if (!string.IsNullOrEmpty(persistentData.manualDeltaPath))
                    manualDelta = HeightMapIO.Load(persistentData.manualDeltaPath);
            }
            else
            {
                // 2回目以降: Unity 標準ツールによる差分を manualDelta に取り込む
                // new_manualDelta[i] = currentTerrain[i] - oldGenerated[i]
                float[] oldGenerated = HeightMapIO.Load(persistentData.generatedHeightPath);
                float[] currentTerrain = TerrainApplier.ReadHeights(
                    terrain, profile.heightmapResolution);

                int size = profile.heightmapResolution * profile.heightmapResolution;
                manualDelta = new float[size];
                for (int i = 0; i < size; i++)
                    manualDelta[i] = currentTerrain[i] - oldGenerated[i];

                // protectedMask が存在する場合: 保護領域の最終高さを維持する
                // 保護された delta = currentTerrain[i] - newGenerated[i]（再生成後も高さ変わらず）
                float[] protectedMask = null;
                if (!string.IsNullOrEmpty(persistentData.protectedMaskPath))
                    protectedMask = HeightMapIO.Load(persistentData.protectedMaskPath);

                if (protectedMask != null)
                {
                    for (int i = 0; i < size; i++)
                    {
                        float pm = protectedMask[i];
                        if (pm <= 0f) continue;
                        float protectedDelta = currentTerrain[i] - generated[i];
                        manualDelta[i] = Mathf.Lerp(manualDelta[i], protectedDelta, pm);
                    }
                }
            }
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

            // manualDelta が null（パス未設定など）のときはゼロ配列を初期ファイルとして保存
            if (manualDelta == null && !string.IsNullOrEmpty(persistentData.manualDeltaPath))
            {
                manualDelta = new float[profile.heightmapResolution * profile.heightmapResolution];
                HeightMapIO.Save(manualDelta, persistentData.manualDeltaPath);
            }
            else if (manualDelta != null && !string.IsNullOrEmpty(persistentData.manualDeltaPath))
            {
                HeightMapIO.Save(manualDelta, persistentData.manualDeltaPath);
            }

            UnityEditor.EditorUtility.SetDirty(persistentData);
#endif
        }
    }
}
