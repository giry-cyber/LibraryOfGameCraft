using LibraryOfGamecraft.Noise;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// fBm（フラクタルブラウン運動）ノイズ。SimplexNoise を複数オクターブ重ねて地形らしい起伏を生成する。
    /// </summary>
    public class FractalNoise2D : INoise2D
    {
        private readonly int _seed;
        private readonly float _noiseScale;
        private readonly int _octaves;
        private readonly float _persistence;
        private readonly float _lacunarity;

        public FractalNoise2D(int seed, float noiseScale, int octaves, float persistence, float lacunarity)
        {
            _seed = seed;
            _noiseScale = noiseScale;
            _octaves = octaves;
            _persistence = persistence;
            _lacunarity = lacunarity;
        }

        public float Sample(float x, float z)
        {
            SimplexNoise.SetSeed(_seed);

            float value = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;

            for (int i = 0; i < _octaves; i++)
            {
                float sx = x * frequency * _noiseScale;
                float sz = z * frequency * _noiseScale;
                // [-1,1] を [0,1] に正規化してから振幅を掛ける
                value += (SimplexNoise.Evaluate(sx, sz) * 0.5f + 0.5f) * amplitude;
                maxValue += amplitude;
                amplitude *= _persistence;
                frequency *= _lacunarity;
            }

            return value / maxValue;
        }
    }
}
