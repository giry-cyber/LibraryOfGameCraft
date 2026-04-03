using UnityEngine;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// マスク配列に対して数値指定の範囲編集を適用する静的ユーティリティ（Phase 3A / 3B 共通）。
    /// mask[i] を maskValue へ SmoothStep falloff でブレンドする。
    /// </summary>
    public static class MaskEditor
    {
        public static void Apply(
            float[] mask,
            int resolution,
            float tileSize,
            Vector2 tileOrigin,
            ShapeType shapeType,
            float centerX,
            float centerZ,
            float radius,
            float rectWidth,
            float rectHeight,
            float maskValue,
            float falloff)
        {
            float halfW = rectWidth  * 0.5f;
            float halfH = rectHeight * 0.5f;

            for (int zi = 0; zi < resolution; zi++)
            {
                float worldZ = tileOrigin.y + (zi / (float)(resolution - 1)) * tileSize;
                for (int xi = 0; xi < resolution; xi++)
                {
                    float worldX = tileOrigin.x + (xi / (float)(resolution - 1)) * tileSize;

                    float t = CalcT(shapeType, worldX, worldZ, centerX, centerZ, radius, halfW, halfH);
                    if (t >= 1f) continue;

                    float tAdj  = Mathf.InverseLerp(1f - Mathf.Clamp01(falloff), 1f, t);
                    float weight = 1f - tAdj * tAdj * (3f - 2f * tAdj); // SmoothStep

                    int idx = zi * resolution + xi;
                    mask[idx] = Mathf.Clamp01(Mathf.Lerp(mask[idx], maskValue, weight));
                }
            }
        }

        private static float CalcT(
            ShapeType shape,
            float px, float pz,
            float cx, float cz,
            float radius,
            float halfW, float halfH)
        {
            if (shape == ShapeType.Circle)
            {
                float dx = px - cx;
                float dz = pz - cz;
                return Mathf.Sqrt(dx * dx + dz * dz) / radius;
            }
            else
            {
                float nx = Mathf.Abs(px - cx) / halfW;
                float nz = Mathf.Abs(pz - cz) / halfH;
                return Mathf.Max(nx, nz);
            }
        }
    }
}
