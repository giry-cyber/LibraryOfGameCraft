using UnityEngine;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// manualDeltaMap に対して数値指定の範囲編集を適用する静的ユーティリティ（Phase 2A）。
    /// Raise / Lower / Flatten の各モードを Circle / Rectangle 形状・SmoothStep falloff で処理する。
    /// </summary>
    public static class ManualDeltaEditor
    {
        /// <summary>
        /// manualDelta 配列を直接書き換える。
        /// generatedHeight は Flatten モードでのみ参照する。
        /// </summary>
        public static void Apply(
            float[] manualDelta,
            float[] generatedHeight,
            int resolution,
            float tileSize,
            Vector2 tileOrigin,
            float heightScale,
            EditMode mode,
            ShapeType shapeType,
            float centerX,
            float centerZ,
            float radius,
            float rectWidth,
            float rectHeight,
            float strengthMeters,
            float targetHeightMeters,
            float falloff)
        {
            float strengthNorm  = strengthMeters     / heightScale;
            float targetNorm    = targetHeightMeters / heightScale;
            float halfW         = rectWidth  * 0.5f;
            float halfH         = rectHeight * 0.5f;

            for (int zi = 0; zi < resolution; zi++)
            {
                float worldZ = tileOrigin.y + (zi / (float)(resolution - 1)) * tileSize;
                for (int xi = 0; xi < resolution; xi++)
                {
                    float worldX = tileOrigin.x + (xi / (float)(resolution - 1)) * tileSize;

                    // 正規化距離 t (0=中心, 1=境界, >1=範囲外)
                    float t = CalcT(shapeType, worldX, worldZ, centerX, centerZ, radius, halfW, halfH);
                    if (t >= 1f) continue;

                    // falloff パラメータ: 0=ハードエッジ, 1=中心からフルSmoothStep
                    // falloff=0 のとき硬い境界 (weight=1 一定)
                    // falloff=1 のとき中心から境界まで SmoothStep で減衰
                    float tAdj  = Mathf.InverseLerp(1f - Mathf.Clamp01(falloff), 1f, t);
                    float weight = 1f - tAdj * tAdj * (3f - 2f * tAdj); // SmoothStep

                    int idx = zi * resolution + xi;
                    ApplyMode(manualDelta, generatedHeight, idx, mode, weight, strengthNorm, targetNorm);
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
                // Rectangle: Chebyshev 正規化距離
                float nx = Mathf.Abs(px - cx) / halfW;
                float nz = Mathf.Abs(pz - cz) / halfH;
                return Mathf.Max(nx, nz);
            }
        }

        private static void ApplyMode(
            float[] delta,
            float[] generated,
            int idx,
            EditMode mode,
            float weight,
            float strengthNorm,
            float targetNorm)
        {
            switch (mode)
            {
                case EditMode.Raise:
                    delta[idx] += strengthNorm * weight;
                    break;

                case EditMode.Lower:
                    delta[idx] -= strengthNorm * weight;
                    break;

                case EditMode.Flatten:
                    // finalHeight = generated + delta を targetNorm に近づける
                    float current = generated[idx] + delta[idx];
                    delta[idx] += (targetNorm - current) * weight;
                    break;
            }
        }
    }
}
