using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    /// <summary>
    /// OuterWall 面に壁モジュールをグリッド配置する（仕様書 6.4〜6.6）。
    /// </summary>
    public static class WallGenerator
    {
        public static List<(GeneratedElement element, GameObject go)> Generate(
            AnalyzedFace face,
            BuildingModuleCatalog catalog,
            GenerationSettings settings,
            Transform parent)
        {
            var result = new List<(GeneratedElement, GameObject)>();

            var primary = catalog.primaryWallModule;
            if (primary == null)
            {
                Debug.LogWarning("[WallGenerator] Catalog の Primary Wall Module が null です。ModuleEntry をセットしてください。");
                return result;
            }
            if (primary.prefab == null)
            {
                Debug.LogWarning($"[WallGenerator] ModuleEntry '{primary.name}' の prefab フィールドが null です。Prefab をセットしてください。");
                return result;
            }

            float moduleWidth  = primary.nominalSize.x;
            float moduleHeight = settings.floorHeight;

            // 壁面ローカル座標系（仕様書 6.4）
            var forward = face.faceNormal;
            var up      = Vector3.up;
            var right   = Vector3.Cross(up, forward).normalized;

            // 頂点を right/up 軸へ投影
            float uMin = float.MaxValue, uMax = float.MinValue;
            float hMin = float.MaxValue, hMax = float.MinValue;

            foreach (var v in face.worldVertices)
            {
                float u = Vector3.Dot(v, right);
                float h = Vector3.Dot(v, up);
                if (u < uMin) uMin = u;
                if (u > uMax) uMax = u;
                if (h < hMin) hMin = h;
                if (h > hMax) hMax = h;
            }

            float faceWidth  = uMax - uMin;
            float faceHeight = hMax - hMin;

            // 奥行きオフセット（Y 汚染排除、仕様書 6.4）
            var forwardXZ = new Vector3(face.faceNormal.x, 0f, face.faceNormal.z).normalized;
            var p0 = face.worldVertices[0];
            var faceDepthOffset = forwardXZ * Vector3.Dot(p0, forwardXZ);

            // baseElevation の決定
            float baseElev = settings.useAutomaticBaseElevation
                ? face.bounds.min.y    // フォールバック（BuildingGenerator 側で建物全体の min.y を渡す）
                : settings.baseElevation;

            int colCount = Mathf.FloorToInt(faceWidth / moduleWidth);
            float remainder = faceWidth - colCount * moduleWidth;

            // TrimWall の採用チェック（余りが maxTrimCoverageRatio * moduleWidth 以下の場合）
            BuildingModuleEntry trimEntry = FindEntryByRole(catalog.wallModules, ModuleRole.TrimWall);
            bool useTrim = trimEntry != null
                        && trimEntry.prefab != null
                        && remainder > 0f
                        && remainder <= settings.maxTrimCoverageRatio * moduleWidth
                        && trimEntry.nominalSize.x <= remainder; // はみ出す場合は不採用（仕様書 6.6 Step2）

            // AdjustableWall の採用チェック（余りが scaleRange 内でスケール可能な場合）
            BuildingModuleEntry adjustEntry = FindEntryByRole(catalog.wallModules, ModuleRole.AdjustableWall);
            bool useAdjust = !useTrim
                          && adjustEntry != null
                          && adjustEntry.prefab != null
                          && remainder > 0f
                          && IsScalable(remainder, adjustEntry);

            // Step 3: PrimaryWallModule の軽微スケール救済
            bool usePrimaryScale = !useTrim && !useAdjust
                                && primary.allowScale
                                && colCount > 0
                                && remainder > 0f
                                && IsScalable(faceWidth / colCount, primary);

            Quaternion rot = Quaternion.LookRotation(forward, up);

            for (int floor = 0; floor < settings.floorCount; floor++)
            {
                float cellMinY   = baseElev + floor * moduleHeight;
                float placementY = cellMinY;

                for (int col = 0; col < colCount; col++)
                {
                    var pivotWorld = ComputePivot(faceDepthOffset, right, uMin, col, moduleWidth, up, placementY);

                    float scaleX = usePrimaryScale
                        ? faceWidth / colCount / moduleWidth
                        : 1f;

                    var entry = new GeneratedElement
                    {
                        elementId       = GeneratedObjectRegistry.IssueElementId(),
                        sourceKind      = SourceKind.WallCell,
                        sourceId        = face.sourceId,
                        role            = ModuleRole.PrimaryWall,
                        moduleAssetId   = primary.moduleId,
                        generationGroup = $"wall_{face.sourceId}_floor{floor}"
                    };

                    var go = SpawnModule(primary.prefab, pivotWorld, rot, parent);
                    go.transform.localScale = new Vector3(
                        go.transform.localScale.x * scaleX,
                        go.transform.localScale.y,
                        go.transform.localScale.z);
                    entry.bounds = new Bounds(pivotWorld, new Vector3(moduleWidth * scaleX, moduleHeight, primary.nominalSize.z));

                    result.Add((entry, go));
                }

                // 余り処理
                if (remainder > 0f)
                {
                    bool placed = false;

                    if (useAdjust)
                    {
                        float rightEdgeU = uMin + colCount * moduleWidth;
                        var pivotWorld = ComputePivot(faceDepthOffset, right, rightEdgeU, 0, remainder, up, placementY);
                        float scaleX = remainder / adjustEntry.nominalSize.x;

                        var entry = new GeneratedElement
                        {
                            elementId       = GeneratedObjectRegistry.IssueElementId(),
                            sourceKind      = SourceKind.WallCell,
                            sourceId        = face.sourceId,
                            role            = ModuleRole.AdjustableWall,
                            moduleAssetId   = adjustEntry.moduleId,
                            generationGroup = $"wall_{face.sourceId}_floor{floor}"
                        };
                        var go = SpawnModule(adjustEntry.prefab, pivotWorld, rot, parent);
                        go.transform.localScale = new Vector3(
                            go.transform.localScale.x * scaleX,
                            go.transform.localScale.y,
                            go.transform.localScale.z);
                        entry.bounds = new Bounds(pivotWorld, new Vector3(remainder, moduleHeight, adjustEntry.nominalSize.z));
                        result.Add((entry, go));
                        placed = true;
                    }

                    if (!placed && useTrim)
                    {
                        // 右端揃え（仕様書 6.6 Step2）
                        float trimWidth   = trimEntry.nominalSize.x;
                        float rightEdge   = uMin + faceWidth;
                        float trimLeftU   = rightEdge - trimWidth;
                        float trimCenterU = trimLeftU + trimWidth * 0.5f;

                        var pivotWorld = faceDepthOffset
                            + right * trimCenterU
                            + Vector3.up * placementY;

                        var entry = new GeneratedElement
                        {
                            elementId       = GeneratedObjectRegistry.IssueElementId(),
                            sourceKind      = SourceKind.WallCell,
                            sourceId        = face.sourceId,
                            role            = ModuleRole.TrimWall,
                            moduleAssetId   = trimEntry.moduleId,
                            generationGroup = $"wall_{face.sourceId}_floor{floor}"
                        };
                        var go = SpawnModule(trimEntry.prefab, pivotWorld, rot, parent);
                        entry.bounds = new Bounds(pivotWorld, new Vector3(trimWidth, moduleHeight, trimEntry.nominalSize.z));
                        result.Add((entry, go));
                        placed = true;
                    }

                    if (!placed && !usePrimaryScale)
                    {
                        Debug.LogWarning(
                            $"[WallGenerator] Face {face.sourceId} floor {floor}: 余り処理に失敗しました（余り={remainder:F3}m）。");
                    }
                }
            }

            return result;
        }

        // ---- helpers ----

        private static Vector3 ComputePivot(
            Vector3 faceDepthOffset, Vector3 right, float uMin,
            int col, float moduleWidth, Vector3 up, float placementY)
        {
            return faceDepthOffset
                 + right * (uMin + col * moduleWidth + moduleWidth * 0.5f)
                 + Vector3.up * placementY;
        }

        private static bool IsScalable(float neededWidth, BuildingModuleEntry entry)
        {
            float scale = neededWidth / entry.nominalSize.x;
            return scale >= entry.scaleRange.x && scale <= entry.scaleRange.y;
        }

        private static BuildingModuleEntry FindEntryByRole(List<BuildingModuleEntry> modules, ModuleRole role)
        {
            foreach (var m in modules)
                if (m != null && m.role == role) return m;
            return null;
        }

        private static GameObject SpawnModule(GameObject prefab, Vector3 worldPos, Quaternion rot, Transform parent)
        {
            var go = Object.Instantiate(prefab, worldPos, rot, parent);
            return go;
        }
    }
}
