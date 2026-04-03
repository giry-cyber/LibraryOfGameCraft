using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LibraryOfGamecraft.Terrain.Editor
{
    public class TerrainToolWindow : EditorWindow
    {
        private enum Tab { Generate = 0, Batch = 1, Edit = 2, Vegetation = 3 }

        private Tab _currentTab = Tab.Generate;

        // ---- Edit タブ ----
        private enum EditInputMode { NumericRange, SceneBrush }
        private EditInputMode _editInputMode = EditInputMode.NumericRange;
        private EditMode      _editMode              = EditMode.Raise;
        private float         _editStrengthMeters    = 10f;
        private float         _editFalloff           = 0.5f;
        private float         _editTargetHeightMeters = 50f;
        // Phase 2A: Numeric Range
        private float         _editCenterX    = 0f;
        private float         _editCenterZ    = 0f;
        private ShapeType     _editShapeType  = ShapeType.Circle;
        private float         _editRadius     = 50f;
        private float         _editRectWidth  = 100f;
        private float         _editRectHeight = 100f;

        // Phase 2B: Scene Brush
        private float         _brushRadius          = 50f;
        private bool          _brushActive          = false;
        private Vector3       _brushWorldPos;
        private Vector3       _brushLastApplyPos;
        private float[]       _brushGeneratedCache;
        private float[]       _brushDeltaCache;

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

        private static readonly string[] TabLabels = { "Generate", "Batch", "Edit", "Vegetation" };

        private void OnEnable()
        {
            // コンパイル後に SerializedObject は失われるため再構築する
            if (_profile != null && _profileSO == null)
                _profileSO = new SerializedObject(_profile);
            if (_batchConfig != null && _batchConfigSO == null)
                _batchConfigSO = new SerializedObject(_batchConfig);

            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            _brushActive = false;
            _brushGeneratedCache = null;
            _brushDeltaCache = null;
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
                case Tab.Edit:
                    DrawEditTab();
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
        //  Edit タブ (Phase 2A: Numeric Range Edit)
        // ================================================================

        private void DrawEditTab()
        {
            EditorGUILayout.LabelField("Edit Input Mode", EditorStyles.boldLabel);
            _editInputMode = (EditInputMode)GUILayout.Toolbar((int)_editInputMode,
                new[] { "Numeric Range", "Scene Brush" });
            EditorGUILayout.Space();

            // ---- 共通セクション ----
            EditorGUILayout.LabelField("Common", EditorStyles.boldLabel);
            _editMode           = (EditMode)EditorGUILayout.EnumPopup("Mode", _editMode);
            _editStrengthMeters = EditorGUILayout.FloatField("Strength (m)", _editStrengthMeters);
            _editFalloff        = EditorGUILayout.Slider("Falloff", _editFalloff, 0f, 1f);
            if (_editMode == EditMode.Flatten)
                _editTargetHeightMeters = EditorGUILayout.FloatField("Target Height (m)", _editTargetHeightMeters);

            EditorGUILayout.Space();

            switch (_editInputMode)
            {
                case EditInputMode.NumericRange:
                    DrawNumericRangeSection();
                    break;
                case EditInputMode.SceneBrush:
                    DrawSceneBrushSection();
                    break;
            }
        }

        private void DrawNumericRangeSection()
        {
            EditorGUILayout.LabelField("Numeric Range Edit", EditorStyles.boldLabel);

            _editCenterX   = EditorGUILayout.FloatField("Center X (m)", _editCenterX);
            _editCenterZ   = EditorGUILayout.FloatField("Center Z (m)", _editCenterZ);
            _editShapeType = (ShapeType)EditorGUILayout.EnumPopup("Shape", _editShapeType);

            if (_editShapeType == ShapeType.Circle)
            {
                _editRadius = EditorGUILayout.FloatField("Radius (m)", _editRadius);
            }
            else
            {
                _editRectWidth  = EditorGUILayout.FloatField("Width (m)",  _editRectWidth);
                _editRectHeight = EditorGUILayout.FloatField("Height (m)", _editRectHeight);
            }

            EditorGUILayout.Space();

            bool canApply = _targetTerrain != null && _persistentData != null && _profile != null;

            if (!canApply)
                EditorGUILayout.HelpBox("Generate タブで Terrain・Persistent Data・Profile を設定してください", MessageType.Warning);

            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("Apply Edit", GUILayout.Height(32f)))
                    ExecuteEditApply();
            }
        }

        private void ExecuteEditApply()
        {
            // generated を読み込む（読み取り専用）
            float[] generated = HeightMapIO.Load(_persistentData.generatedHeightPath);
            if (generated == null)
            {
                Debug.LogError("[TerrainTool] generatedHeightMap が見つかりません。先に Generate を実行してください。");
                return;
            }

            // Unity 標準ツールによる差分を manualDelta に吸収する
            // new_manualDelta[i] = currentTerrain[i] - generated[i]
            float[] currentTerrain = TerrainApplier.ReadHeights(_targetTerrain, _profile.heightmapResolution);
            int size = _profile.heightmapResolution * _profile.heightmapResolution;
            float[] manualDelta = new float[size];
            for (int i = 0; i < size; i++)
                manualDelta[i] = currentTerrain[i] - generated[i];

            // 編集を適用
            ManualDeltaEditor.Apply(
                manualDelta,
                generated,
                _profile.heightmapResolution,
                _profile.tileSizeMeters,
                new Vector2(_tileOriginX, _tileOriginZ),
                _profile.heightScale,
                _editMode,
                _editShapeType,
                _editCenterX,
                _editCenterZ,
                _editRadius,
                _editRectWidth,
                _editRectHeight,
                _editStrengthMeters,
                _editTargetHeightMeters,
                _editFalloff);

            // manualDelta を保存
            HeightMapIO.Save(manualDelta, _persistentData.manualDeltaPath);

            // Terrain に反映
            TerrainApplier.Apply(
                _targetTerrain,
                generated,
                manualDelta,
                _profile.heightmapResolution,
                _profile.tileSizeMeters,
                _profile.heightScale);

            Debug.Log("[TerrainTool] Edit Apply 完了");
        }

        private void DrawSceneBrushSection()
        {
            EditorGUILayout.LabelField("Scene Brush Edit", EditorStyles.boldLabel);
            _brushRadius = EditorGUILayout.FloatField("Brush Radius (m)", _brushRadius);

            bool canBrush = _targetTerrain != null && _persistentData != null && _profile != null;
            if (!canBrush)
            {
                EditorGUILayout.HelpBox("Generate タブで Terrain・Persistent Data・Profile を設定してください", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();

            string toggleLabel = _brushActive ? "ブラシ無効化" : "ブラシ有効化";
            if (GUILayout.Button(toggleLabel, GUILayout.Height(32f)))
            {
                _brushActive = !_brushActive;
                if (_brushActive)
                    LoadBrushCaches();
                else
                    FlushBrushCaches();

                SceneView.RepaintAll();
            }

            if (_brushActive)
                EditorGUILayout.HelpBox("SceneView でマウスドラッグして塗ってください。\nブラシを無効化すると保存されます。", MessageType.Info);
        }

        private void LoadBrushCaches()
        {
            _brushGeneratedCache = HeightMapIO.Load(_persistentData.generatedHeightPath);
            if (_brushGeneratedCache == null)
            {
                Debug.LogError("[TerrainTool] generatedHeightMap が見つかりません。先に Generate を実行してください。");
                _brushActive = false;
                return;
            }

            _brushDeltaCache = HeightMapIO.Load(_persistentData.manualDeltaPath);
            if (_brushDeltaCache == null)
                _brushDeltaCache = new float[_profile.heightmapResolution * _profile.heightmapResolution];
        }

        private void FlushBrushCaches()
        {
            if (_brushDeltaCache == null) return;

            HeightMapIO.Save(_brushDeltaCache, _persistentData.manualDeltaPath);

            TerrainApplier.Apply(
                _targetTerrain,
                _brushGeneratedCache,
                _brushDeltaCache,
                _profile.heightmapResolution,
                _profile.tileSizeMeters,
                _profile.heightScale);

            Debug.Log("[TerrainTool] ブラシ編集を保存しました");
            _brushGeneratedCache = null;
            _brushDeltaCache = null;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_brushActive || _currentTab != Tab.Edit || _editInputMode != EditInputMode.SceneBrush)
                return;
            if (_targetTerrain == null || _profile == null || _brushGeneratedCache == null)
                return;

            Event e = Event.current;

            // Terrain 平面への Raycast
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane terrainPlane = new Plane(Vector3.up, _targetTerrain.transform.position);
            if (!terrainPlane.Raycast(ray, out float dist))
                return;

            _brushWorldPos = ray.GetPoint(dist);

            // ブラシ円を描画
            Handles.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            Handles.DrawWireDisc(_brushWorldPos, Vector3.up, _brushRadius);
            Handles.color = new Color(0.2f, 0.8f, 1f, 0.1f);
            Handles.DrawSolidDisc(_brushWorldPos, Vector3.up, _brushRadius);

            // マウスボタン押下中に塗る（最小移動距離で間引き）
            bool isMouseDown = e.type == EventType.MouseDown && e.button == 0;
            bool isMouseDrag = e.type == EventType.MouseDrag && e.button == 0;

            if (isMouseDown || isMouseDrag)
            {
                float minStep = _brushRadius * 0.25f;
                if (isMouseDown || Vector3.Distance(_brushWorldPos, _brushLastApplyPos) >= minStep)
                {
                    _brushLastApplyPos = _brushWorldPos;

                    ManualDeltaEditor.Apply(
                        _brushDeltaCache,
                        _brushGeneratedCache,
                        _profile.heightmapResolution,
                        _profile.tileSizeMeters,
                        new Vector2(_tileOriginX, _tileOriginZ),
                        _profile.heightScale,
                        _editMode,
                        ShapeType.Circle,
                        _brushWorldPos.x,
                        _brushWorldPos.z,
                        _brushRadius,
                        0f, 0f,
                        _editStrengthMeters,
                        _editTargetHeightMeters,
                        _editFalloff);

                    // リアルタイムで Terrain に反映（保存は FlushBrushCaches で行う）
                    TerrainApplier.Apply(
                        _targetTerrain,
                        _brushGeneratedCache,
                        _brushDeltaCache,
                        _profile.heightmapResolution,
                        _profile.tileSizeMeters,
                        _profile.heightScale);

                    e.Use();
                }
            }

            // 常にリペイント（ブラシカーソル追従）
            sceneView.Repaint();
            Repaint();
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
