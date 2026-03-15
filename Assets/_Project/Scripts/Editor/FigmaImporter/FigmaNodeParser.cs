using System;
using System.Collections.Generic;
using UnityEngine;

namespace FigmaImporter
{
    /// <summary>
    /// JSON文字列から FigmaNode ツリーをパースするユーティリティ。
    /// Unity の JsonUtility は Dictionary 非対応なので、
    /// 最低限必要なフィールドだけを持つシリアライズクラスで対応する。
    /// </summary>
    public static class FigmaNodeParser
    {
        // ---- 対応するノードタイプ ----
        private static readonly HashSet<string> SupportedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FRAME", "GROUP", "RECTANGLE", "TEXT", "COMPONENT", "INSTANCE", "SECTION"
        };

        // ---- スキップするノードタイプ ----
        private static readonly HashSet<string> SkippedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "VECTOR", "BOOLEAN_OPERATION", "STAR", "LINE", "ELLIPSE", "POLYGON",
            "SLICE", "CONNECTOR", "SHAPE_WITH_HOLES"
        };

        /// <summary>JSON 文字列をパースして FigmaFile を返す。失敗時は null。</summary>
        public static FigmaFile ParseFile(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[FigmaImporter] JSON が空です。");
                return null;
            }

            try
            {
                var file = JsonUtility.FromJson<FigmaFile>(json);
                if (file == null || file.document == null)
                {
                    Debug.LogWarning("[FigmaImporter] パース結果が空です。JSON 構造を確認してください。");
                    return null;
                }
                return file;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FigmaImporter] JSON パースエラー: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// ノードが処理対象かどうかを返す。
        /// visible=false のノードは常にスキップ。
        /// </summary>
        public static bool IsSupported(FigmaNode node)
        {
            if (node == null) return false;
            if (!node.visible) return false;
            if (SkippedTypes.Contains(node.type))
            {
                Debug.Log($"[FigmaImporter] スキップ: {node.name} ({node.type})");
                return false;
            }
            return true;
        }

        /// <summary>最初の SOLID fill の色を返す。なければ white。</summary>
        public static Color GetFirstSolidColor(FigmaNode node)
        {
            if (node.fills == null) return Color.white;
            foreach (var fill in node.fills)
            {
                if (fill.visible && fill.type == "SOLID" && fill.color != null)
                    return ToUnityColor(fill.color, fill.opacity);
            }
            return Color.white;
        }

        public static Color ToUnityColor(FigmaColor c, float extraAlpha = 1f)
        {
            if (c == null) return Color.white;
            return new Color(c.r, c.g, c.b, c.a * extraAlpha);
        }
    }
}
