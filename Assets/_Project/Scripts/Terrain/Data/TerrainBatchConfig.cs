using System;
using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// 複数タイルを一括生成するためのバッチ設定。
    /// タイルごとのシーンパス・tileOrigin・PersistentData のマッピングを保持する。
    /// </summary>
    [CreateAssetMenu(menuName = "LibraryOfGamecraft/Terrain/Batch Config")]
    public class TerrainBatchConfig : ScriptableObject
    {
        public TerrainGenerationProfile profile;
        public List<TileBatchEntry> tiles = new List<TileBatchEntry>();
    }

    [Serializable]
    public class TileBatchEntry
    {
        public string label;
        public string scenePath;
        public Vector2 tileOrigin;
        public TerrainPersistentData persistentData;
    }
}
