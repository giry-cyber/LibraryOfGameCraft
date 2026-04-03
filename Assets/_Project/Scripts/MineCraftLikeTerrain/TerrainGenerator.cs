using UnityEngine;
using LibraryOfGamecraft.Noise;

namespace LibraryOfGamecraft.MineCraftLikeTerrain
{
    /// <summary>
    /// ノイズを使いチャンクのボクセルデータを生成する静的クラス。
    /// </summary>
    public static class TerrainGenerator
    {
        // ---- デフォルト設定キャッシュ ----------------------------------------

        private static TerrainSettings s_default;

        private static TerrainSettings Default
        {
            get
            {
                if (s_default == null)
                {
                    s_default = ScriptableObject.CreateInstance<TerrainSettings>();
                    // surfaceNoise / mountainNoise / caveNoise は null でも動作する
                }
                return s_default;
            }
        }

        // ---- 公開 API --------------------------------------------------------

        /// <summary>
        /// チャンク座標 (チャンク単位) を受け取り ChunkData を生成して返す。
        /// </summary>
        /// <param name="chunkPos">チャンクグリッド座標 (x=チャンク列, y=チャンク行)</param>
        /// <param name="settings">地形設定。null のときデフォルト値を使用。</param>
        public static ChunkData Generate(Vector2Int chunkPos, TerrainSettings settings = null)
        {
            var cfg  = settings ?? Default;
            var data = new ChunkData();

            // シードをノイズライブラリに反映
            SimplexNoise.SetSeed(cfg.worldSeed);

            int baseX = chunkPos.x * ChunkData.Width;
            int baseZ = chunkPos.y * ChunkData.Depth;

            for (int lz = 0; lz < ChunkData.Depth;  lz++)
            for (int lx = 0; lx < ChunkData.Width;  lx++)
            {
                int wx = baseX + lx;
                int wz = baseZ + lz;

                // ---- 地表高さ ------------------------------------------------
                float surfaceH = SampleSurfaceHeight(wx, wz, cfg);
                int   surfaceY = Mathf.Clamp(
                    Mathf.RoundToInt(surfaceH), 2, ChunkData.Height - 2);

                // ---- バイオーム判定 ------------------------------------------
                float temp   = SimplexNoise.Evaluate01(wx * cfg.biomeFrequency,
                                                       wz * cfg.biomeFrequency);
                bool isDesert = temp > cfg.desertThreshold;
                bool isSnowy  = temp < cfg.snowThreshold
                             && surfaceY > cfg.baseHeight + cfg.heightAmplitude * 0.4f;

                // ---- 縦方向ループ --------------------------------------------
                for (int y = 0; y < ChunkData.Height; y++)
                {
                    data.SetBlock(lx, y, lz, DetermineBlock(
                        wx, y, wz, surfaceY, isDesert, isSnowy, cfg));
                }

                // ---- 海水を充填 ----------------------------------------------
                if (!isDesert)
                {
                    for (int y = surfaceY + 1; y <= cfg.seaLevel; y++)
                    {
                        if (data.GetBlock(lx, y, lz) == BlockType.Air)
                            data.SetBlock(lx, y, lz, BlockType.Water);
                    }
                }
            }

            return data;
        }

        // ---- 内部ヘルパー ---------------------------------------------------

        private static float SampleSurfaceHeight(int wx, int wz, TerrainSettings cfg)
        {
            // 地表 fBm
            float nx = wx * cfg.surfaceFrequency;
            float nz = wz * cfg.surfaceFrequency;
            float base01 = cfg.surfaceNoise != null
                ? FractalNoise.Evaluate01(nx, nz, cfg.surfaceNoise)
                : FractalNoise.Evaluate01(nx, nz);

            // 山岳リッジド
            float mx = wx * cfg.mountainFrequency;
            float mz = wz * cfg.mountainFrequency;
            float ridge = cfg.mountainNoise != null
                ? FractalNoise.Ridged(mx, mz, cfg.mountainNoise)
                : FractalNoise.Ridged(mx, mz);

            return cfg.baseHeight
                 + base01  * cfg.heightAmplitude
                 + ridge   * cfg.mountainAmplitude;
        }

        private static BlockType DetermineBlock(
            int wx, int y, int wz,
            int surfaceY, bool isDesert, bool isSnowy,
            TerrainSettings cfg)
        {
            if (y == 0)
                return BlockType.Bedrock;

            if (y > surfaceY)
                return BlockType.Air;

            // 洞窟カービング (地表近くは洞窟を生成しない)
            if (y < surfaceY - 3 && y < cfg.caveMaxHeight)
            {
                float freq = cfg.caveFrequency;
                float cave = cfg.caveNoise != null
                    ? FractalNoise.Evaluate01(wx * freq, y * freq, wz * freq, cfg.caveNoise)
                    : SimplexNoise.Evaluate01(wx * freq, y * freq, wz * freq);
                if (cave > cfg.caveThreshold)
                    return BlockType.Air;
            }

            // レイヤー割り当て
            int depth = surfaceY - y; // 地表からの深さ

            if (depth == 0) // 地表
            {
                if (isDesert) return BlockType.Sand;
                if (isSnowy)  return BlockType.Snow;
                return BlockType.Grass;
            }
            if (depth <= 3)
                return isDesert ? BlockType.Sand : BlockType.Dirt;
            if (depth <= 5)
                return BlockType.Gravel;

            return BlockType.Stone;
        }
    }
}
