using UnityEngine;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// ノイズサンプラーを用いてハイトマップ（float配列、[0,1]正規化）を生成する。
    /// タイル境界が隣接タイルと一致するようワールド座標基準でサンプリングする。
    /// </summary>
    public class TerrainGenerator
    {
        private readonly INoise2D _noise;
        private readonly IDomainWarp2D _domainWarp;

        public TerrainGenerator(INoise2D noise, IDomainWarp2D domainWarp = null)
        {
            _noise = noise;
            _domainWarp = domainWarp;
        }

        public float[] Generate(TerrainGenerationProfile profile, Vector2 tileOrigin)
        {
            int R = profile.heightmapResolution;
            float[] heights = new float[R * R];

            for (int z = 0; z < R; z++)
            {
                for (int x = 0; x < R; x++)
                {
                    // ワールド座標基準サンプリング（タイル境界一致のため）
                    float worldX = tileOrigin.x + (x / (float)(R - 1)) * profile.tileSizeMeters;
                    float worldZ = tileOrigin.y + (z / (float)(R - 1)) * profile.tileSizeMeters;

                    float sx = worldX;
                    float sz = worldZ;

                    if (_domainWarp != null)
                    {
                        Vector2 warp = _domainWarp.Warp(worldX, worldZ);
                        sx += warp.x;
                        sz += warp.y;
                    }

                    heights[z * R + x] = _noise.Sample(sx, sz);
                }
            }

            return heights;
        }
    }
}
