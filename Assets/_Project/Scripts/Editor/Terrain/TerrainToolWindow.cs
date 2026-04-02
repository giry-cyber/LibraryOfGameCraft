using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LibraryOfGamecraft.Terrain.Editor
{
    public class TerrainToolWindow : EditorWindow
    {
        private enum Tab { Generate = 0, Batch = 1, Paint = 2, Vegetation = 3 }

        private Tab _currentTab = Tab.Generate;

        // ---- Generate タブ ----
        private TerrainGenerationProfile _profile;
        private SerializedObject _profileSO;
        private UnityEngine.Terrain _targetTerrain;
        private TerrainPersistentData _persistentData;
        private float _tileOriginX = 0f;
        private float _tileOriginZ = 0f;
        private bool _isDirty;

        // ---- Batch タブ ----
        private TerrainBatchConfig _batchConfig;
        private SerializedObject _batchConfigSO;
        private Vector2 _batchScrollPos;

        private static readonly string[] TabLabels = { "Generate", "Batch", "Paint", "Vegetation" };

        private void OnEnable()
        {
            // コンパイル後に SerializedObject は失われるため再構築する
            if (_profile != null && _profileSO == null)
                _profileSO = new SerializedObject(_profile);
            if (_batchConfig != null && _batchConfigSO == null)
                _batchConfigSO = new SerializedObject(_batchConfig);
        }

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
                case Tab.Batch:
                    DrawBatchTab();
                    break;
                default:
                    EditorGUILayout.HelpBox("Coming Soon", MessageType.Info);
                    break;
            }
        }

        // ================================================================
        //  Generate タブ
        // ================================================================

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
            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<TerrainGenerationProfile>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            _profile = asset;
            _profileSO = new SerializedObject(_profile);
            _isDirty = false;
        }

        private void DuplicateProfile()
        {
            if (_profile == null) return;

            string srcPath = AssetDatabase.GetAssetPath(_profile);
            string dstPath = AssetDatabase.GenerateUniqueAssetPath(srcPath);
            AssetDatabase.CopyAsset(srcPath, dstPath);
            AssetDatabase.SaveAssets();

            _profile = AssetDatabase.LoadAssetAtPath<TerrainGenerationProfile>(dstPath);
            _profileSO = new SerializedObject(_profile);
            _isDirty = false;
        }

        // ================================================================
        //  Batch タブ
        // ================================================================

        private void DrawBatchTab()
        {
            EditorGUILayout.LabelField("Batch Config", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var newConfig = (TerrainBatchConfig)EditorGUILayout.ObjectField(
                "Batch Config", _batchConfig, typeof(TerrainBatchConfig), false);
            if (EditorGUI.EndChangeCheck())
            {
                _batchConfig = newConfig;
                _batchConfigSO = _batchConfig != null ? new SerializedObject(_batchConfig) : null;
            }

            if (GUILayout.Button("New Batch Config"))
                CreateNewBatchConfig();

            EditorGUILayout.Space();

            if (_batchConfig == null)
            {
                EditorGUILayout.HelpBox("Batch Config を設定してください", MessageType.Warning);
                return;
            }

            _batchConfigSO.Update();

            // Profile フィールド
            EditorGUILayout.PropertyField(_batchConfigSO.FindProperty("profile"));

            EditorGUILayout.Space();

            // タイルリスト
            EditorGUILayout.LabelField("Tiles", EditorStyles.boldLabel);
            var tilesProp = _batchConfigSO.FindProperty("tiles");
            _batchScrollPos = EditorGUILayout.BeginScrollView(_batchScrollPos, GUILayout.MaxHeight(300f));
            for (int i = 0; i < tilesProp.arraySize; i++)
            {
                var entry = tilesProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("label"), GUIContent.none);
                if (GUILayout.Button("×", GUILayout.Width(24f)))
                {
                    tilesProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(entry.FindPropertyRelative("scenePath"), new GUIContent("Scene Path"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("tileOrigin"), new GUIContent("Tile Origin"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("persistentData"), new GUIContent("Persistent Data"));

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2f);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("+ タイルを追加"))
            {
                tilesProp.InsertArrayElementAtIndex(tilesProp.arraySize);
                var newEntry = tilesProp.GetArrayElementAtIndex(tilesProp.arraySize - 1);
                newEntry.FindPropertyRelative("label").stringValue = $"tile_{tilesProp.arraySize - 1:00}";
                newEntry.FindPropertyRelative("scenePath").stringValue = "";
                newEntry.FindPropertyRelative("persistentData").objectReferenceValue = null;
            }

            _batchConfigSO.ApplyModifiedProperties();

            EditorGUILayout.Space();

            // バリデーション
            bool canGenerate = _batchConfig.profile != null && _batchConfig.tiles.Count > 0;
            if (_batchConfig.profile == null)
                EditorGUILayout.HelpBox("Profile を設定してください", MessageType.Warning);
            else if (_batchConfig.tiles.Count == 0)
                EditorGUILayout.HelpBox("タイルを1つ以上追加してください", MessageType.Warning);

            using (new EditorGUI.DisabledScope(!canGenerate))
            {
                if (GUILayout.Button($"Generate All ({_batchConfig.tiles.Count} tiles)", GUILayout.Height(36f)))
                    ExecuteBatchGenerate();
            }
        }

        private void ExecuteBatchGenerate()
        {
            var tiles = _batchConfig.tiles;
            int total = tiles.Count;

            // 現在開いているシーンを記憶しておき、完了後に戻す
            string originalScenePath = EditorSceneManager.GetActiveScene().path;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    var entry = tiles[i];
                    string label = string.IsNullOrEmpty(entry.label) ? $"tile_{i}" : entry.label;

                    bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                        "Batch Generate",
                        $"[{i + 1}/{total}] {label}",
                        (float)i / total);

                    if (cancelled) break;

                    if (string.IsNullOrEmpty(entry.scenePath))
                    {
                        Debug.LogWarning($"[BatchGenerate] {label}: scenePath が未設定のためスキップ");
                        continue;
                    }
                    if (entry.persistentData == null)
                    {
                        Debug.LogWarning($"[BatchGenerate] {label}: PersistentData が未設定のためスキップ");
                        continue;
                    }

                    // シーンを開く
                    var scene = EditorSceneManager.OpenScene(entry.scenePath, OpenSceneMode.Single);

                    // シーン内の Terrain を取得
                    var terrain = FindTerrainInScene(scene);
                    if (terrain == null)
                    {
                        Debug.LogWarning($"[BatchGenerate] {label}: シーン内に Terrain が見つからないためスキップ");
                        continue;
                    }

                    // Generate
                    var service = new TerrainBuildService();
                    service.Build(terrain, _batchConfig.profile, entry.persistentData, entry.tileOrigin);

                    // シーンを保存
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[BatchGenerate] {label} 完了");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                // 元のシーンに戻す
                if (!string.IsNullOrEmpty(originalScenePath))
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

                Debug.Log("[BatchGenerate] 全タイル処理完了");
            }
        }

        private static UnityEngine.Terrain FindTerrainInScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var t = root.GetComponentInChildren<UnityEngine.Terrain>(false);
                if (t != null) return t;
            }
            return null;
        }

        private void CreateNewBatchConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "New Batch Config",
                "TerrainBatchConfig",
                "asset",
                "保存先を選択してください");
            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<TerrainBatchConfig>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            _batchConfig = asset;
            _batchConfigSO = new SerializedObject(_batchConfig);
        }
    }
}
