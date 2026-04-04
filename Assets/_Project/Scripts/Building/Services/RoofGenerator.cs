using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    /// <summary>
    /// Roof 面ごとに FlatRoof モジュールを 1 枚配置する（仕様書 6.7）。
    /// </summary>
    public static class RoofGenerator
    {
        public static List<(GeneratedElement element, GameObject go)> Generate(
            AnalyzedFace face,
            BuildingModuleCatalog catalog,
            BuildingAuthoring authoring,
            Transform parent)
        {
            var result = new List<(GeneratedElement, GameObject)>();

            if (catalog.roofModules == null || catalog.roofModules.Count == 0)
            {
                Debug.LogWarning("[RoofGenerator] RoofModules が空です。");
                return result;
            }

            // priority 昇順で最初に有効な FlatRoof エントリを採用
            BuildingModuleEntry roofEntry = null;
            int bestPriority = int.MaxValue;
            foreach (var m in catalog.roofModules)
            {
                if (m == null || m.prefab == null) continue;
                if (m.role != ModuleRole.FlatRoof) continue;
                if (m.priority < bestPriority) { bestPriority = m.priority; roofEntry = m; }
            }

            if (roofEntry == null)
            {
                Debug.LogWarning("[RoofGenerator] 有効な FlatRoof モジュールが見つかりません。");
                return result;
            }

            // 屋根面サイズを求める
            var right = Vector3.right;
            var forward_world = Vector3.forward;

            // BuildingAuthoring.transform.forward の水平投影（仕様書 6.8）
            var buildingForward = authoring.transform.forward;
            var horizontalForward = new Vector3(buildingForward.x, 0f, buildingForward.z);
            if (horizontalForward.sqrMagnitude < 0.001f)
                horizontalForward = Vector3.forward;
            else
                horizontalForward.Normalize();

            var roofRight = Vector3.Cross(Vector3.up, horizontalForward).normalized;

            // 頂点を roofRight / horizontalForward に投影してサイズ計算
            float uMin = float.MaxValue, uMax = float.MinValue;
            float vMin = float.MaxValue, vMax = float.MinValue;

            foreach (var v in face.worldVertices)
            {
                float u = Vector3.Dot(v, roofRight);
                float fv = Vector3.Dot(v, horizontalForward);
                if (u < uMin) uMin = u;
                if (u > uMax) uMax = u;
                if (fv < vMin) vMin = fv;
                if (fv > vMax) vMax = fv;
            }

            float roofWidth  = uMax - uMin;
            float roofDepth  = vMax - vMin;

            // ピボット（面中心）
            var pivotWorld = face.faceCenter;

            // 回転: +Y = worldUp, +Z = horizontalForward（仕様書 6.8 屋根）
            var rot = Quaternion.LookRotation(horizontalForward, Vector3.up);

            var go = Object.Instantiate(roofEntry.prefab, pivotWorld, rot, parent);

            // allowScale = true, adjustableAxes = XZ なので面サイズに合わせてスケール
            if (roofEntry.allowScale && roofEntry.nominalSize.x > 0f && roofEntry.nominalSize.z > 0f)
            {
                float scaleX = roofWidth  / roofEntry.nominalSize.x;
                float scaleZ = roofDepth  / roofEntry.nominalSize.z;
                go.transform.localScale = new Vector3(scaleX, 1f, scaleZ);
            }

            var element = new GeneratedElement
            {
                elementId       = GeneratedObjectRegistry.IssueElementId(),
                sourceKind      = SourceKind.RoofCell,
                sourceId        = face.sourceId,
                role            = ModuleRole.FlatRoof,
                moduleAssetId   = roofEntry.moduleId,
                generationGroup = $"roof_{face.sourceId}",
                bounds          = new Bounds(pivotWorld, new Vector3(roofWidth, roofEntry.nominalSize.y, roofDepth))
            };

            result.Add((element, go));
            return result;
        }
    }
}
