namespace LibraryOfGamecraft.Terrain
{
    public class FractalNoise2DFactory : INoiseFactory
    {
        private readonly float _noiseScale;
        private readonly int _octaves;
        private readonly float _persistence;
        private readonly float _lacunarity;

        public FractalNoise2DFactory(float noiseScale, int octaves, float persistence, float lacunarity)
        {
            _noiseScale = noiseScale;
            _octaves = octaves;
            _persistence = persistence;
            _lacunarity = lacunarity;
        }

        public INoise2D Create(int seed)
        {
            return new FractalNoise2D(seed, _noiseScale, _octaves, _persistence, _lacunarity);
        }
    }
}
