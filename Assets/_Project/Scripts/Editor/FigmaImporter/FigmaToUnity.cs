using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FigmaImporter
{
    /// <summary>
    /// FigmaNode ツリーを uGUI GameObject 階層に変換する。
    /// 未対応ノードはログを出してスキップする。
    /// </summary>
    public static class FigmaToUnity
    {
        // ---- 公開 API ----

        /// <summary>
        /// Figma ファイル全体を Unity に配置する。
        /// document 直下の最初の CANVAS（Page）を対象にする。
        /// </summary>
        public static void BuildFromFile(FigmaFile file, Canvas targetCanvas)
        {
            if (file?.document == null)
            {
                Debug.LogError("[FigmaImporter] FigmaFile が無効です。");
                return;
            }

            // document → CANVAS（Page）を探す
            var pages = file.document.children;
            if (pages == null || pages.Count == 0)
            {
                Debug.LogWarning("[FigmaImporter] ページが見つかりません。");
                return;
            }

            // 最初のページのみ対象（複数ページは未対応）
            var page = pages[0];
            Debug.Log($"[FigmaImporter] ページ: {page.name}");

            if (page.children == null) return;

            var root = targetCanvas.GetComponent<RectTransform>();
            foreach (var topNode in page.children)
                BuildNode(topNode, root, page);
        }

        // ---- 内部 ----

        private static void BuildNode(FigmaNode node, RectTransform parent, FigmaNode pageNode)
        {
            if (!FigmaNodeParser.IsSupported(node)) return;

            switch (node.type.ToUpperInvariant())
            {
                case "FRAME":
                case "GROUP":
                case "COMPONENT":
                case "INSTANCE":
                case "SECTION":
                    BuildContainer(node, parent, pageNode);
                    break;

                case "RECTANGLE":
                    BuildImage(node, parent, pageNode);
                    break;

                case "TEXT":
                    BuildText(node, parent, pageNode);
                    break;

                default:
                    Debug.Log($"[FigmaImporter] 未対応タイプをスキップ: {node.name} ({node.type})");
                    break;
            }
        }

        // ---- コンテナ (FRAME / GROUP) ----
        private static void BuildContainer(FigmaNode node, RectTransform parent, FigmaNode pageNode)
        {
            var go = new GameObject(node.name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);

            // 背景色があれば Image を付ける
            var color = FigmaNodeParser.GetFirstSolidColor(node);
            if (color != Color.white || (node.fills != null && node.fills.Count > 0))
            {
                var img = go.AddComponent<Image>();
                img.color = color;
                img.raycastTarget = false;
            }

            ApplyRect(rt, node, pageNode);
            ApplyOpacity(go, node);

            // 子を再帰処理
            if (node.children == null) return;
            foreach (var child in node.children)
                BuildNode(child, rt, pageNode);
        }

        // ---- 矩形 (RECTANGLE) ----
        private static void BuildImage(FigmaNode node, RectTransform parent, FigmaNode pageNode)
        {
            var go = new GameObject(node.name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = FigmaNodeParser.GetFirstSolidColor(node);
            img.raycastTarget = false;

            ApplyRect(rt, node, pageNode);
            ApplyOpacity(go, node);
        }

        // ---- テキスト (TEXT) ----
        private static void BuildText(FigmaNode node, RectTransform parent, FigmaNode pageNode)
        {
            var go = new GameObject(node.name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);

            // TextMeshPro がプロジェクトに含まれている前提
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = node.characters ?? "";
            tmp.raycastTarget = false;

            if (node.style != null)
            {
                tmp.fontSize = node.style.fontSize;
                tmp.color = FigmaNodeParser.GetFirstSolidColor(node); // fills[0] をテキスト色として利用

                tmp.alignment = node.style.textAlignHorizontal switch
                {
                    "CENTER" => TextAlignmentOptions.Center,
                    "RIGHT"  => TextAlignmentOptions.Right,
                    _        => TextAlignmentOptions.Left,
                };
            }

            ApplyRect(rt, node, pageNode);
            ApplyOpacity(go, node);
        }

        // ---- ユーティリティ ----

        /// <summary>
        /// Figma の絶対座標を親相対の RectTransform に変換する。
        /// Figma は左上原点・Y下向き、Unity は中心原点・Y上向き。
        /// </summary>
        private static void ApplyRect(RectTransform rt, FigmaNode node, FigmaNode pageNode)
        {
            var bb = node.absoluteBoundingBox;
            if (bb == null) return;

            // ページ座標系での左上を (0,0) に正規化
            float pageX = pageNode.absoluteBoundingBox?.x ?? 0f;
            float pageY = pageNode.absoluteBoundingBox?.y ?? 0f;

            float localX = bb.x - pageX;
            float localY = bb.y - pageY;

            rt.anchorMin = Vector2.up;   // 左上アンカー
            rt.anchorMax = Vector2.up;
            rt.pivot     = Vector2.up;

            rt.sizeDelta = new Vector2(bb.width, bb.height);
            rt.anchoredPosition = new Vector2(localX, -localY);  // Y を反転
        }

        /// <summary>opacity が 1 未満のとき CanvasGroup を追加する。</summary>
        private static void ApplyOpacity(GameObject go, FigmaNode node)
        {
            if (node.opacity >= 1f) return;
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = node.opacity;
            cg.blocksRaycasts = false;
        }
    }
}
