using UnityEditor;
using UnityEngine;

namespace LibraryOfGamecraft.Terrain.Editor
{
    public class TerrainToolWindow : EditorWindow
    {
        private enum Tab { Generate = 0, Paint = 1, Vegetation = 2 }

        private Tab _currentTab = Tab.Generate;

        private TerrainGenerationProfile _profile;
        private SerializedObject _profileSO;

        private UnityEngine.Terrain _targetTerrain;
        private TerrainPersistentData _persistentData;
        private float _tileOriginX = 0f;
        private float _tileOriginZ = 0f;

        private bool _isDirty;

        private static readonly string[] TabLabels = { "Generate", "Paint", "Vegetation" };

        [MenuItem("Tools/LibraryOfGamecraft/Terrain Tool")]
        public static void OpenWindow()
        {
            var window = GetWindow<TerrainToolWindow>("Terrain Tool");
            window.minSize = new Vector2(380f, 500f);
            window.Show();
        }

        private void OnGUI()
        {
            _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, TabLabels);
            EditorGUILayout.Space();

            switch (_currentTab)
            {
                case Tab.Generate:
                    DrawGenerateTab();
                    break;
                default:
                    EditorGUILayout.HelpBox("Coming Soon", MessageType.Info);
                    break;
            }
        }

        private void DrawGenerateTab()
        {
            EditorGUILayout.LabelField("Generation Profile", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var newProfile = (TerrainGenerationProfile)EditorGUILayout.ObjectField(
                "Profile", _profile, typeof(TerrainGenerationProfile), false);
            if (EditorGUI.EndChangeCheck())
            {
                _profile = newProfile;
                _profileSO = _profile != null ? new SerializedObject(_profile) : null;
                _isDirty = false;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Profile"))
                CreateNewProfile();
            if (GUILayout.Button("Duplicate Profile"))
                DuplicateProfile();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (_profile == null)
            {
                EditorGUILayout.HelpBox("Profile を設定してください", MessageType.Warning);
                return;
            }

            // SerializedObject でプロファイルフィールドを描画（Undo/Redo 対応）
            _profileSO.Update();

            EditorGUILayout.LabelField("Profile Settings", EditorStyles.boldLabel);
            DrawProfileFields();

            if (_profileSO.ApplyModifiedProperties())
                _isDirty = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target Terrain", EditorStyles.boldLabel);

            _targetTerrain = (UnityEngine.Terrain)EditorGUILayout.ObjectField(
                "Terrain", _targetTerrain, typeof(UnityEngine.Terrain), true);

            _persistentData = (TerrainPersistentData)EditorGUILayout.ObjectField(
                "Persistent Data", _persistentData, typeof(TerrainPersistentData), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tile Origin", EditorStyles.boldLabel);
            _tileOriginX = EditorGUILayout.FloatField("Origin X", _tileOriginX);
            _tileOriginZ = EditorGUILayout.FloatField("Origin Z", _tileOriginZ);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_targetTerrain == null || _persistentData == null))
            {
                if (GUILayout.Button("Generate", GUILayout.Height(32f)))
                    ExecuteGenerate();
            }

            if (_targetTerrain == null || _persistentData == null)
                EditorGUILayout.HelpBox("Terrain と Persistent Data を設定してください", MessageType.Warning);

            if (_isDirty)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("未保存の変更があります", MessageType.Warning);
            }
        }

        private void DrawProfileFields()
        {
            EditorGUILayout.PropertyField(_profileSO.FindProperty("seed"));
            EditorGUILayout.PropertyField(_profileSO.FindProperty("tileSizeMeters"));
            EditorGUILayout.PropertyField(_profileSO.FindProperty("heightmapResolution"));
            EditorGUILayout.PropertyField(_profileSO.FindProperty("heightScale"));
            EditorGUILayout.PropertyField(_profileSO.FindProperty("noiseScale"));
            EditorGUILayout.PropertyField(_profileSO.FindProperty("octaves"));
            EditorGUILayout.PropertyField(_profileSO.FindProperty("persistence"));
            EditorGUILayout.PropertyField(_profileSO.FindProperty("lacunarity"));
            EditorGUILayout.PropertyField(_profileSO.FindProperty("useDomainWarp"));
            if (_profile.useDomainWarp)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_profileSO.FindProperty("domainWarpStrength"));
                EditorGUILayout.PropertyField(_profileSO.FindProperty("domainWarpScale"));
                EditorGUI.indentLevel--;
            }
        }

        private void ExecuteGenerate()
        {
            var service = new TerrainBuildService();
            service.Build(
                _targetTerrain,
                _profile,
                _persistentData,
                new Vector2(_tileOriginX, _tileOriginZ));

            _isDirty = false;
            Debug.Log("[TerrainTool] Generate 完了");
        }

        private void CreateNewProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "New Generation Profile",
                "TerrainGenerationProfile",
                "asset",
                "保存先を選択してください");

            if (string.IsNullOrEmpty(path))
                return;

            var asset = CreateInstance<TerrainGenerationProfile>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            _profile = asset;
            _profileSO = new SerializedObject(_profile);
            _isDirty = false;
        }

        private void DuplicateProfile()
        {
            if (_profile == null)
                return;

            string srcPath = AssetDatabase.GetAssetPath(_profile);
            string dstPath = AssetDatabase.GenerateUniqueAssetPath(srcPath);
            AssetDatabase.CopyAsset(srcPath, dstPath);
            AssetDatabase.SaveAssets();

            _profile = AssetDatabase.LoadAssetAtPath<TerrainGenerationProfile>(dstPath);
            _profileSO = new SerializedObject(_profile);
            _isDirty = false;
        }
    }
}
