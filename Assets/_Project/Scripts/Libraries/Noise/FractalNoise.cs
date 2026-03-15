using UnityEngine;

namespace LibraryOfGamecraft.Noise
{
    /// <summary>
    /// フラクタルブラウン運動 (fBm) ベースのノイズユーティリティ。
    /// <see cref="SimplexNoise"/> を複数オクターブ重ね合わせて自然な起伏を生成する。
    /// </summary>
    public static class FractalNoise
    {
        // ---- 2D ---------------------------------------------------------------

        /// <summary>
        /// 2D fBm ノイズを返す。戻り値は概ね [-1, 1]。
        /// </summary>
        /// <param name="x">X 座標</param>
        /// <param name="y">Y 座標</param>
        /// <param name="settings">ノイズパラメータ。null の場合はデフォルト値を使用。</param>
        public static float Evaluate(float x, float y, NoiseSettings settings = null)
        {
            if (settings != null) SimplexNoise.SetSeed(settings.seed);

            float freq      = settings?.frequency  ?? 1f;
            float amp       = settings?.amplitude  ?? 1f;
            float persist   = settings?.persistence ?? 0.5f;
            float lacun     = settings?.lacunarity  ?? 2f;
            int   octaves   = settings?.octaves     ?? 4;
            var   offset    = settings?.offset      ?? Vector3.zero;

            float value     = 0f;
            float maxValue  = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float nx = (x + offset.x) * freq;
                float ny = (y + offset.y) * freq;
                value    += SimplexNoise.Evaluate(nx, ny) * amp;
                maxValue += amp;
                amp  *= persist;
                freq *= lacun;
            }

            return value / maxValue;
        }

        /// <inheritdoc cref="Evaluate(float, float, NoiseSettings)"/>
        public static float Evaluate(Vector2 pos, NoiseSettings settings = null)
            => Evaluate(pos.x, pos.y, settings);

        // ---- 3D ---------------------------------------------------------------

        /// <summary>
        /// 3D fBm ノイズを返す。戻り値は概ね [-1, 1]。
        /// </summary>
        public static float Evaluate(float x, float y, float z, NoiseSettings settings = null)
        {
            if (settings != null) SimplexNoise.SetSeed(settings.seed);

            float freq      = settings?.frequency   ?? 1f;
            float amp       = settings?.amplitude   ?? 1f;
            float persist   = settings?.persistence ?? 0.5f;
            float lacun     = settings?.lacunarity  ?? 2f;
            int   octaves   = settings?.octaves     ?? 4;
            var   offset    = settings?.offset      ?? Vector3.zero;

            float value     = 0f;
            float maxValue  = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float nx = (x + offset.x) * freq;
                float ny = (y + offset.y) * freq;
                float nz = (z + offset.z) * freq;
                value    += SimplexNoise.Evaluate(nx, ny, nz) * amp;
                maxValue += amp;
                amp  *= persist;
                freq *= lacun;
            }

            return value / maxValue;
        }

        /// <inheritdoc cref="Evaluate(float, float, float, NoiseSettings)"/>
        public static float Evaluate(Vector3 pos, NoiseSettings settings = null)
            => Evaluate(pos.x, pos.y, pos.z, settings);

        // ---- Normalized variants ----------------------------------------------

        /// <summary>2D fBm ノイズを [0, 1] に正規化して返す。</summary>
        public static float Evaluate01(float x, float y, NoiseSettings settings = null)
            => Evaluate(x, y, settings) * 0.5f + 0.5f;

        /// <inheritdoc cref="Evaluate01(float, float, NoiseSettings)"/>
        public static float Evaluate01(Vector2 pos, NoiseSettings settings = null)
            => Evaluate01(pos.x, pos.y, settings);

        /// <summary>3D fBm ノイズを [0, 1] に正規化して返す。</summary>
        public static float Evaluate01(float x, float y, float z, NoiseSettings settings = null)
            => Evaluate(x, y, z, settings) * 0.5f + 0.5f;

        /// <inheritdoc cref="Evaluate01(float, float, float, NoiseSettings)"/>
        public static float Evaluate01(Vector3 pos, NoiseSettings settings = null)
            => Evaluate01(pos.x, pos.y, pos.z, settings);

        // ---- Terrain-oriented helpers -----------------------------------------

        /// <summary>
        /// リッジドノイズ (山の稜線のような形状) を返す。戻り値は [0, 1]。
        /// </summary>
        public static float Ridged(float x, float y, NoiseSettings settings = null)
        {
            float raw = Evaluate(x, y, settings);
            return 1f - Mathf.Abs(raw);
        }

        /// <inheritdoc cref="Ridged(float, float, NoiseSettings)"/>
        public static float Ridged(float x, float y, float z, NoiseSettings settings = null)
        {
            float raw = Evaluate(x, y, z, settings);
            return 1f - Mathf.Abs(raw);
        }

        /// <summary>
        /// タービュランスノイズ (絶対値 fBm) を返す。戻り値は [0, 1]。
        /// </summary>
        public static float Turbulence(float x, float y, NoiseSettings settings = null)
            => Mathf.Abs(Evaluate(x, y, settings));

        /// <inheritdoc cref="Turbulence(float, float, NoiseSettings)"/>
        public static float Turbulence(float x, float y, float z, NoiseSettings settings = null)
            => Mathf.Abs(Evaluate(x, y, z, settings));

        // ---- Texture utilities ------------------------------------------------

        /// <summary>
        /// 2D グレースケールテクスチャを生成して返す。
        /// </summary>
        /// <param name="width">テクスチャ幅 (px)</param>
        /// <param name="height">テクスチャ高さ (px)</param>
        /// <param name="settings">ノイズパラメータ</param>
        /// <param name="mode">ノイズの種類</param>
        public static Texture2D GenerateTexture(
            int width, int height,
            NoiseSettings settings = null,
            NoiseTextureMode mode = NoiseTextureMode.FBm)
        {
            var tex    = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            float invW = 1f / width;
            float invH = 1f / height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x * invW;
                    float ny = y * invH;

                    float v = mode switch
                    {
                        NoiseTextureMode.FBm        => Evaluate01(nx, ny, settings),
                        NoiseTextureMode.Ridged      => Ridged(nx, ny, settings),
                        NoiseTextureMode.Turbulence  => Turbulence(nx, ny, settings),
                        _                            => Evaluate01(nx, ny, settings)
                    };

                    pixels[y * width + x] = new Color(v, v, v, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }

    /// <summary><see cref="FractalNoise.GenerateTexture"/> で生成するノイズの種類。</summary>
    public enum NoiseTextureMode
    {
        /// <summary>標準的なフラクタルブラウン運動</summary>
        FBm,
        /// <summary>稜線状のリッジドノイズ</summary>
        Ridged,
        /// <summary>乱流状のタービュランスノイズ</summary>
        Turbulence
    }
}
