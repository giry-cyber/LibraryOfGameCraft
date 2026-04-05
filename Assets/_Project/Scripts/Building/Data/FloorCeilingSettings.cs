using System;
using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    [Serializable]
    public class FloorCeilingSettings
    {
        public Material material;
        public bool generateCollider = false;
        public float thickness = 0.1f;
        /// <summary>面法線方向のスカラーオフセット量</summary>
        public float offset = 0f;
    }
}
