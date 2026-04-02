using UnityEngine;

namespace LibraryOfGamecraft.Terrain
{
    [CreateAssetMenu(menuName = "LibraryOfGamecraft/Terrain/Persistent Data")]
    public class TerrainPersistentData : ScriptableObject
    {
        public string terrainGuid;
        public int width;
        public int height;
        public string generatedHeightPath;
        public string manualDeltaPath;
        public string protectedMaskPath;
        public string noVegetationMaskPath;
        public string flattenMaskPath;

        /// <summary>
        /// GUID を元に各バイナリファイルのパスを一括設定する。
        /// Inspector のコンテキストメニュー（右クリック）からも実行可能。
        /// </summary>
        public void SetPaths(string guid)
        {
            terrainGuid = guid;
            string dir = $"Assets/TerrainToolData/{guid}";
            generatedHeightPath = $"{dir}/generated.bytes";
            manualDeltaPath = $"{dir}/manualDelta.bytes";
            protectedMaskPath = $"{dir}/protectedMask.bytes";
            noVegetationMaskPath = $"{dir}/noVegetationMask.bytes";
            flattenMaskPath = $"{dir}/flatten.bytes";
        }

        [ContextMenu("Apply terrainGuid to Paths")]
        private void ApplyGuidToPaths()
        {
            if (string.IsNullOrEmpty(terrainGuid))
            {
                Debug.LogWarning("[TerrainPersistentData] terrainGuid が未設定です。先に入力してください。");
                return;
            }
            SetPaths(terrainGuid);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log($"[TerrainPersistentData] パスを設定しました: Assets/TerrainToolData/{terrainGuid}/");
        }
    }
}
