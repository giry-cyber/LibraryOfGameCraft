using UnityEditor;
using UnityEngine;
using LibraryOfGamecraft.Building;

namespace LibraryOfGamecraft.Editor.Building
{
    /// <summary>
    /// BuildingAuthoring の カスタム Inspector。
    /// GenerationSettings の編集と Rebuild All ボタンを提供する（仕様書 4.7, 6章）。
    /// </summary>
    [CustomEditor(typeof(BuildingAuthoring))]
    public class BuildingAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty _generationSettings;
        private SerializedProperty _catalog;
        private SerializedProperty _showSemanticOverlay;

        private void OnEnable()
        {
            _generationSettings  = serializedObject.FindProperty("generationSettings");
            _catalog             = serializedObject.FindProperty("catalog");
            _showSemanticOverlay = serializedObject.FindProperty("showSemanticOverlay");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var authoring = (BuildingAuthoring)target;

            // ---- Catalog ----
            EditorGUILayout.LabelField("カタログ", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_catalog, new GUIContent("Module Catalog"));

            EditorGUILayout.Space(4);

            // ---- Generation Settings ----
            EditorGUILayout.LabelField("生成設定", EditorStyles.boldLabel);
            DrawGenerationSettings(_generationSettings);

            EditorGUILayout.Space(4);

            // ---- Overlay ----
            EditorGUILayout.PropertyField(_showSemanticOverlay, new GUIContent("Semantic オーバーレイ表示"));

            EditorGUILayout.Space(8);

            // ---- Rebuild All ----
            using (new EditorGUI.DisabledScope(authoring.catalog == null))
            {
                if (GUILayout.Button("Rebuild All", GUILayout.Height(32)))
                {
                    Undo.RecordObject(authoring.SemanticStore, "Rebuild All");
                    Undo.RecordObject(authoring.Registry, "Rebuild All");
                    BuildingGenerator.RebuildAll(authoring);
                    EditorUtility.SetDirty(authoring);
                    EditorUtility.SetDirty(authoring.SemanticStore);
                    SceneView.RepaintAll();
                }
            }

            if (authoring.catalog == null)
                EditorGUILayout.HelpBox("BuildingModuleCatalog を設定してください。", MessageType.Warning);

            EditorGUILayout.Space(4);

            // ---- Semantic Editor ----
            if (GUILayout.Button("Semantic Editor を開く"))
                SemanticEditorWindow.Open(authoring);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawGenerationSettings(SerializedProperty prop)
        {
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("floorCount"),   new GUIContent("フロア数"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("floorHeight"),  new GUIContent("フロア高さ"));

            var useAuto = prop.FindPropertyRelative("useAutomaticBaseElevation");
            EditorGUILayout.PropertyField(useAuto, new GUIContent("baseElevation 自動算出"));
            if (!useAuto.boolValue)
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("baseElevation"), new GUIContent("  baseElevation"));

            EditorGUILayout.PropertyField(prop.FindPropertyRelative("roofHeightEpsilon"),     new GUIContent("Roof 判定 Epsilon"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("minLastFloorHeightRatio"), new GUIContent("最終階 最小高さ比"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("generateCeiling"),          new GUIContent("天井を生成する"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("generateInterFloorSlabs"), new GUIContent("フロア間スラブを生成する"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("maxTrimCoverageRatio"),    new GUIContent("TrimWall 最大被覆率"));
        }
    }
}
