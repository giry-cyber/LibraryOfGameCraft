using System;
using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    public enum SourceKind
    {
        Face,
        WallCell,
        FloorCell,
        CeilingCell,
        RoofCell
    }

    [Serializable]
    public class GeneratedElement
    {
        public int elementId;
        public SourceKind sourceKind;
        public int sourceId;
        public ModuleRole role;
        public string moduleAssetId;
        public string generationGroup;
        public bool isLocked;
        public Bounds bounds;
    }
}
