using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace LibraryOfGamecraft.Building
{
    /// <summary>
    /// Rebuild All のオーケストレーター（仕様書 8.5）。
    /// Shape 解析 → Semantic 再マッピング → 生成オブジェクト配置 → Registry 再構築 を一括実行する。
    /// </summary>
    public static class BuildingGenerator
    {
        /// <summary>
        /// Rebuild All を実行する。
        /// Editor から呼ぶ前提なので DestroyImmediate を使用する。
        /// </summary>
        public static void RebuildAll(BuildingAuthoring authoring)
        {
            var pbMesh = authoring.GetComponent<ProBuilderMesh>();
            if (pbMesh == null)
            {
                Debug.LogError("[BuildingGenerator] ProBuilderMesh が見つかりません。");
                return;
            }

            if (authoring.catalog == null)
            {
                Debug.LogError("[BuildingGenerator] BuildingModuleCatalog が未設定です。");
                return;
            }

            var store    = authoring.SemanticStore;
            var registry = authoring.Registry;
            var settings = authoring.generationSettings;

            // Step 1-2: 旧オブジェクト破棄・Registry クリア（仕様書 8.5）
            registry.Clear();

            // Step 3: Shape 再解析
            var newFaces = FaceSemanticAnalyzer.Analyze(pbMesh);

            // baseElevation の自動算出
            if (settings.useAutomaticBaseElevation)
            {
                var worldBounds = FaceSemanticAnalyzer.ComputeWorldBounds(pbMesh);
                // 再マッピング時に AnalyzedFace へ反映するため一時的に settings に書いておく
                // （WallGenerator は settings.baseElevation を直接参照しないが、
                //   Generate 時に渡す面の bounds.min.y で代替するため問題なし）
                _ = worldBounds.min.y;   // 計算値は WallGenerator 内で face.bounds.min.y を使う
            }

            // Step 4: Semantic 再マッピング（仕様書 9章）
            var oldRecords = store.Records;
            List<FaceSemanticRecord> newRecords;

            if (oldRecords.Count == 0)
            {
                // 初回: 自動分類結果をそのまま記録
                newRecords = new List<FaceSemanticRecord>(newFaces.Count);
                foreach (var f in newFaces)
                    newRecords.Add(FaceSemanticAnalyzer.ToRecord(f));
            }
            else
            {
                // 再ビルド: 手動修正を再マッピング
                newRecords = SemanticRemapper.Remap(oldRecords, newFaces);
            }

            // Step 5-6: SemanticStore を更新
            store.SetRecords(newRecords);

            // 再マッピング後の Semantic を AnalyzedFace に反映
            var recordMap = new Dictionary<int, FaceSemanticRecord>();
            foreach (var r in newRecords)
                recordMap[r.sourceId] = r;

            foreach (var f in newFaces)
            {
                if (recordMap.TryGetValue(f.sourceId, out var r))
                {
                    f.semantic   = r.semantic;
                    f.confidence = r.confidence;
                }
            }

            // Step 7-8: GameObject 生成・Registry 構築
            var generatedRoot = registry.GeneratedRoot;

            // baseElevation の確定値
            float baseElev = settings.useAutomaticBaseElevation
                ? FaceSemanticAnalyzer.ComputeWorldBounds(pbMesh).min.y
                : settings.baseElevation;

            var floorFaces = new List<AnalyzedFace>();

            foreach (var face in newFaces)
            {
                switch (face.semantic)
                {
                    case FaceSemantic.OuterWall:
                        GenerateWall(face, authoring, registry, generatedRoot, baseElev);
                        break;

                    case FaceSemantic.Roof:
                        GenerateRoof(face, authoring, registry, generatedRoot);
                        break;

                    case FaceSemantic.Floor:
                        GenerateFloor(face, authoring, registry, generatedRoot);
                        floorFaces.Add(face);
                        break;

                    case FaceSemantic.CeilingCandidate:
                        if (settings.generateCeiling)
                            GenerateCeiling(face, authoring, registry, generatedRoot);
                        break;
                }
            }

            // フロア間スラブ（2階以上が存在する場合のみ）
            if (settings.generateInterFloorSlabs && settings.floorCount >= 2 && floorFaces.Count > 0)
                GenerateInterFloorSlabs(floorFaces, authoring, registry, generatedRoot, baseElev);

            Debug.Log($"[BuildingGenerator] Rebuild 完了: {registry.Elements.Count} 個の要素を生成しました。");
        }

        // ---- 各生成デリゲート ----

        private static void GenerateWall(
            AnalyzedFace face, BuildingAuthoring authoring,
            GeneratedObjectRegistry registry, Transform parent, float baseElev)
        {
            // baseElev を GenerationSettings に注入して WallGenerator へ渡す
            var settings = authoring.generationSettings;
            var tempBaseElev = settings.useAutomaticBaseElevation;
            float tempVal    = settings.baseElevation;
            settings.useAutomaticBaseElevation = false;
            settings.baseElevation             = baseElev;

            var pairs = WallGenerator.Generate(face, authoring.catalog, settings, parent);

            settings.useAutomaticBaseElevation = tempBaseElev;
            settings.baseElevation             = tempVal;

            foreach (var (elem, go) in pairs)
                registry.Register(elem, go);
        }

        private static void GenerateRoof(
            AnalyzedFace face, BuildingAuthoring authoring,
            GeneratedObjectRegistry registry, Transform parent)
        {
            var pairs = RoofGenerator.Generate(face, authoring.catalog, authoring, parent);
            foreach (var (elem, go) in pairs)
                registry.Register(elem, go);
        }

        private static void GenerateFloor(
            AnalyzedFace face, BuildingAuthoring authoring,
            GeneratedObjectRegistry registry, Transform parent)
        {
            var (elem, go) = FloorCeilingGenerator.Generate(
                face, authoring.catalog.floorSettings, SourceKind.FloorCell, parent);
            registry.Register(elem, go);
        }

        private static void GenerateCeiling(
            AnalyzedFace face, BuildingAuthoring authoring,
            GeneratedObjectRegistry registry, Transform parent)
        {
            var (elem, go) = FloorCeilingGenerator.Generate(
                face, authoring.catalog.ceilingSettings, SourceKind.CeilingCell, parent);
            registry.Register(elem, go);
        }

        /// <summary>
        /// Floor 面の XZ 形状を各フロア境界高さにコピーしてスラブメッシュを生成する。
        /// floorIndex = 1 〜 floorCount-1 の境界（地面スラブは FloorCell 生成済みのためスキップ）。
        /// </summary>
        private static void GenerateInterFloorSlabs(
            List<AnalyzedFace> floorFaces,
            BuildingAuthoring authoring,
            GeneratedObjectRegistry registry,
            Transform parent,
            float baseElev)
        {
            var settings  = authoring.generationSettings;
            var slabConf  = authoring.catalog.interFloorSlabSettings;

            for (int floorIndex = 1; floorIndex < settings.floorCount; floorIndex++)
            {
                float slabY = baseElev + floorIndex * settings.floorHeight;

                foreach (var srcFace in floorFaces)
                {
                    // Floor 面の XZ 形状を slabY へ投影した合成 AnalyzedFace を作る
                    var slabVerts = new System.Collections.Generic.List<Vector3>(srcFace.worldVertices.Count);
                    foreach (var v in srcFace.worldVertices)
                        slabVerts.Add(new Vector3(v.x, slabY, v.z));

                    var slabCenter = Vector3.zero;
                    foreach (var v in slabVerts) slabCenter += v;
                    slabCenter /= slabVerts.Count;

                    var slabBounds = new Bounds(slabVerts[0], Vector3.zero);
                    for (int i = 1; i < slabVerts.Count; i++) slabBounds.Encapsulate(slabVerts[i]);

                    var syntheticFace = new AnalyzedFace
                    {
                        sourceId      = GeneratedObjectRegistry.IssueElementId(),
                        faceCenter    = slabCenter,
                        faceNormal    = Vector3.up,
                        faceArea      = srcFace.faceArea,
                        bounds        = slabBounds,
                        worldVertices = slabVerts,
                        semantic      = FaceSemantic.Floor,
                        confidence    = 1.0f
                    };

                    var (elem, go) = FloorCeilingGenerator.Generate(syntheticFace, slabConf, SourceKind.FloorCell, parent);
                    elem.generationGroup = $"interFloor_{floorIndex}";
                    registry.Register(elem, go);
                }
            }
        }
    }
}
