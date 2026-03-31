namespace LibraryOfGamecraft.Terrain
{
    public interface INoiseFactory
    {
        INoise2D Create(int seed);
    }
}
