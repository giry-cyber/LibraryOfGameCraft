using UnityEngine;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// 生成ハイトマップと手動デルタを合成して Unity Terrain に適用する静的ユーティリティ。
    /// </summary>
    public static class TerrainApplier
    {
        /// <summary>
        /// Unity Terrain の現在のハイトマップを float 配列として読み取る。
        /// </summary>
        public static float[] ReadHeights(UnityEngine.Terrain terrain, int resolution)
        {
            float[,] h = terrain.terrainData.GetHeights(0, 0, resolution, resolution);
            float[] result = new float[resolution * resolution];
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x++)
                    result[z * resolution + x] = h[z, x];
            return result;
        }

        public static void Apply(
            UnityEngine.Terrain terrain,
            float[] generatedHeights,
            float[] manualDelta,
            int resolution,
            float terrainSizeMeters,
            float heightScale)
        {
            TerrainData terrainData = terrain.terrainData;
            terrainData.heightmapResolution = resolution;
            terrainData.size = new Vector3(terrainSizeMeters, heightScale, terrainSizeMeters);

            // Unity の SetHeights は [z, x] 順で Y-up
            float[,] heights2D = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = z * resolution + x;
                    float delta = manualDelta != null ? manualDelta[i] : 0f;
                    heights2D[z, x] = Mathf.Clamp01(generatedHeights[i] + delta);
                }
            }

            terrainData.SetHeights(0, 0, heights2D);
        }
    }
}
