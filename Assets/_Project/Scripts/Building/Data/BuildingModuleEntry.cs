using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    [CreateAssetMenu(fileName = "ModuleEntry", menuName = "LibraryOfGamecraft/Building/Module Entry")]
    public class BuildingModuleEntry : ScriptableObject
    {
        public string moduleId;
        public ModuleRole role;
        public GameObject prefab;
        /// <summary>壁: X=幅 Y=高さ Z=厚み / 屋根(FlatRoof): X=幅 Y=厚み Z=奥行き</summary>
        public Vector3 nominalSize;
        /// <summary>調整可能な軸 (1=yes, 0=no)。FlatRoof では XZ を指定</summary>
        public Vector3 adjustableAxes;
        public bool allowScale = false;
        public Vector2 scaleRange = new(0.8f, 1.2f);
        public int priority;
    }
}
