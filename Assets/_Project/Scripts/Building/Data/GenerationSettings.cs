using System;

namespace LibraryOfGamecraft.Building
{
    [Serializable]
    public class GenerationSettings
    {
        public int floorCount = 1;
        public float floorHeight = 3f;
        public bool useAutomaticBaseElevation = true;
        public float baseElevation = 0f;   // useAutomaticBaseElevation = false のときのみ使用
        public float roofHeightEpsilon = 0.05f;
        public float minLastFloorHeightRatio = 0.5f;
        public bool generateCeiling = false;
        public float maxTrimCoverageRatio = 0.25f;
        /// <summary>フロア境界ごとに床スラブメッシュを挟むか（1階分は既存 Floor 面で生成済みのため 2階以上で有効）</summary>
        public bool generateInterFloorSlabs = false;
    }
}
