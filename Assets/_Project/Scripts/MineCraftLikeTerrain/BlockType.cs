namespace LibraryOfGamecraft.Terrain
{
    /// <summary>ブロック種別。byte で保存してメモリを節約する。</summary>
    public enum BlockType : byte
    {
        Air     = 0,
        Grass   = 1,
        Dirt    = 2,
        Stone   = 3,
        Sand    = 4,
        Gravel  = 5,
        Bedrock = 6,
        Snow    = 7,
        Log     = 8,
        Leaves  = 9,
        Water   = 10,
    }
}
