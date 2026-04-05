using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    [CreateAssetMenu(fileName = "BuildingModuleCatalog", menuName = "LibraryOfGamecraft/Building/Module Catalog")]
    public class BuildingModuleCatalog : ScriptableObject
    {
        /// <summary>主壁モジュール（原則 allowScale = false）</summary>
        public BuildingModuleEntry primaryWallModule;

        /// <summary>AdjustableWall / TrimWall を含む追加壁モジュール群</summary>
        public List<BuildingModuleEntry> wallModules = new();

        /// <summary>FlatRoof モジュール群</summary>
        public List<BuildingModuleEntry> roofModules = new();

        public FloorCeilingSettings floorSettings = new();
        public FloorCeilingSettings ceilingSettings = new();
        /// <summary>フロア間スラブの設定（generateInterFloorSlabs = true のときのみ使用）</summary>
        public FloorCeilingSettings interFloorSlabSettings = new();
    }
}
