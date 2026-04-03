using UnityEngine;

namespace LibraryOfGamecraft.MineCraftLikeTerrain
{
    /// <summary>
    /// チャンク1つ分のボクセルデータ (純粋データ、MonoBehaviour なし)。
    /// 1次元フラット配列でキャッシュ効率を確保する。
    /// </summary>
    public class ChunkData
    {
        public const int Width  = 16;
        public const int Height = 128;
        public const int Depth  = 16;

        private readonly BlockType[] _blocks = new BlockType[Width * Height * Depth];

        // ---- アクセサ --------------------------------------------------------

        /// <summary>
        /// ローカル座標でブロックを取得する。
        /// 範囲外は <see cref="BlockType.Air"/> を返す。
        /// </summary>
        public BlockType GetBlock(int x, int y, int z)
        {
            if ((uint)x >= Width || (uint)y >= Height || (uint)z >= Depth)
                return BlockType.Air;
            return _blocks[Index(x, y, z)];
        }

        /// <summary>ローカル座標でブロックをセットする。範囲外は無視。</summary>
        public void SetBlock(int x, int y, int z, BlockType type)
        {
            if ((uint)x >= Width || (uint)y >= Height || (uint)z >= Depth)
                return;
            _blocks[Index(x, y, z)] = type;
        }

        /// <summary>Vector3Int オーバーロード。</summary>
        public BlockType GetBlock(Vector3Int p) => GetBlock(p.x, p.y, p.z);

        /// <summary>全ブロックを Air でリセットする (プール再利用時)。</summary>
        public void Clear() => System.Array.Clear(_blocks, 0, _blocks.Length);

        // ---- ユーティリティ --------------------------------------------------

        /// <summary>チャンクの範囲外かどうかを判定する。</summary>
        public static bool IsOutOfBounds(int x, int y, int z)
            => (uint)x >= Width || (uint)y >= Height || (uint)z >= Depth;

        private static int Index(int x, int y, int z)
            => x + Width * (y + Height * z);
    }
}
