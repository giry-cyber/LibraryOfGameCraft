using UnityEngine;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// ブロック1種類の定義。色と UV アトラスタイル位置を持つ。
    /// </summary>
    [System.Serializable]
    public struct BlockDefinition
    {
        public string      name;
        public bool        isSolid;
        /// <summary>頂点カラーモード時の基本色</summary>
        public Color       color;
        /// <summary>UV アトラス上のタイル位置 (格子単位)</summary>
        public Vector2Int  topTile;
        public Vector2Int  sideTile;
        public Vector2Int  bottomTile;
    }

    /// <summary>
    /// ブロック定義のレジストリ。ScriptableObject として保存可能。
    /// 未設定の場合は <see cref="GetDefault"/> のハードコード値にフォールバックする。
    /// </summary>
    [CreateAssetMenu(
        fileName = "BlockRegistry",
        menuName = "LibraryOfGamecraft/Terrain/BlockRegistry")]
    public class BlockRegistry : ScriptableObject
    {
        [Tooltip("UV アトラスの横・縦タイル数 (例: 16×16 アトラスなら 16)")]
        public int atlasSize = 1; // デフォルト1 = アトラスなし (全UV使用)

        [Tooltip("BlockType の順番に合わせて定義を並べる")]
        public BlockDefinition[] blocks = new BlockDefinition[0];

        public static BlockRegistry Instance { get; private set; }

        private void OnEnable()  => Instance = this;
        private void OnDisable() { if (Instance == this) Instance = null; }

        /// <summary>BlockType に対応する定義を返す。未設定ならデフォルト。</summary>
        public BlockDefinition Get(BlockType type)
        {
            int idx = (int)type;
            if (blocks != null && idx < blocks.Length)
                return blocks[idx];
            return GetDefault(type);
        }

        /// <summary>
        /// ScriptableObject が存在しない場合のハードコードデフォルト。
        /// 頂点カラーで即動作する。
        /// </summary>
        public static BlockDefinition GetDefault(BlockType type) => type switch
        {
            BlockType.Grass   => Def("Grass",   true,  new Color(0.40f, 0.72f, 0.30f)),
            BlockType.Dirt    => Def("Dirt",    true,  new Color(0.52f, 0.35f, 0.14f)),
            BlockType.Stone   => Def("Stone",   true,  new Color(0.50f, 0.50f, 0.50f)),
            BlockType.Sand    => Def("Sand",    true,  new Color(0.90f, 0.82f, 0.55f)),
            BlockType.Gravel  => Def("Gravel",  true,  new Color(0.60f, 0.58f, 0.55f)),
            BlockType.Bedrock => Def("Bedrock", true,  new Color(0.18f, 0.18f, 0.18f)),
            BlockType.Snow    => Def("Snow",    true,  new Color(0.92f, 0.95f, 0.98f)),
            BlockType.Log     => Def("Log",     true,  new Color(0.42f, 0.32f, 0.16f)),
            BlockType.Leaves  => Def("Leaves",  true,  new Color(0.25f, 0.55f, 0.20f)),
            BlockType.Water   => Def("Water",   true,  new Color(0.20f, 0.45f, 0.85f)),
            _                 => Def("Air",     false, Color.clear),
        };

        private static BlockDefinition Def(string n, bool solid, Color c) =>
            new BlockDefinition { name = n, isSolid = solid, color = c };
    }
}
