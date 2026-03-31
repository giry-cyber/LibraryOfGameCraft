namespace LibraryOfGamecraft.Terrain
{
    public class SimplexDomainWarp2DFactory : IDomainWarp2DFactory
    {
        private readonly float _warpScale;
        private readonly float _warpStrength;

        public SimplexDomainWarp2DFactory(float warpScale, float warpStrength)
        {
            _warpScale = warpScale;
            _warpStrength = warpStrength;
        }

        public IDomainWarp2D Create(int seed)
        {
            return new SimplexDomainWarp2D(seed, _warpScale, _warpStrength);
        }
    }
}
