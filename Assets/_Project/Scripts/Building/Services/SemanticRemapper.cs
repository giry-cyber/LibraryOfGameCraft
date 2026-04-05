using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    /// <summary>
    /// Rebuild All 時に旧 SemanticStore の手動修正情報を新フェイス群へ再マッピングする（仕様書 9章）。
    /// </summary>
    public static class SemanticRemapper
    {
        // 候補絞り込み閾値（仕様書 9.5）
        private const float MaxNormalAngleDeg  = 5.0f;
        private const float MaxCenterDistance  = 0.25f;
        private const float MaxAreaRelativeDiff = 0.15f;

        // 複合スコアの重み（仕様書 9.7）
        private const float WeightCenter = 0.5f;
        private const float WeightNormal = 0.3f;
        private const float WeightArea   = 0.2f;

        // 一意性チェック（仕様書 9.8）
        private const float EquivalentScoreDelta = 0.05f;

        /// <summary>
        /// 旧 records を参照して newFaces に Semantic を引き継ぐ。
        /// 再マッピング結果を反映した新しい FaceSemanticRecord リストを返す。
        /// </summary>
        public static List<FaceSemanticRecord> Remap(
            IReadOnlyList<FaceSemanticRecord> oldRecords,
            List<AnalyzedFace> newFaces)
        {
            var result = new List<FaceSemanticRecord>(newFaces.Count);

            foreach (var newFace in newFaces)
            {
                var record = TryRemap(oldRecords, newFace);
                result.Add(record);
            }

            return result;
        }

        private static FaceSemanticRecord TryRemap(
            IReadOnlyList<FaceSemanticRecord> oldRecords,
            AnalyzedFace newFace)
        {
            // Phase A: 完全一致検索（仕様書 9.4）
            var candidates = FindExactCandidates(oldRecords, newFace.geometrySignature);

            // Phase B: ±1 bin 近傍検索（仕様書 9.4）
            if (candidates.Count == 0)
                candidates = FindNearbyCandidates(oldRecords, newFace.geometrySignature);

            // boundsSize 後フィルタ（仕様書 9.4）
            candidates = FilterByBoundsSize(candidates, newFace.geometrySignature);

            // 候補なし → 失敗
            if (candidates.Count == 0)
                return FailRecord(newFace);

            // 閾値による絞り込みと複合スコア計算（仕様書 9.5, 9.6, 9.7）
            var scored = ScoreAndFilter(candidates, newFace);

            if (scored.Count == 0)
                return FailRecord(newFace);

            // 一意性チェック（仕様書 9.8）
            scored.Sort((a, b) => a.score.CompareTo(b.score));
            var best = scored[0];

            if (scored.Count >= 2)
            {
                float delta = scored[1].score - best.score;
                if (delta < EquivalentScoreDelta)
                    return FailRecord(newFace);   // 同等スコアが複数 → 一意でないので失敗
            }

            // 成功: 旧 record から引き継ぐ（仕様書 9.3 step6）
            var old = best.record;
            float newConfidence = old.isManualOverride ? 1.0f : 0.75f;

            return new FaceSemanticRecord
            {
                sourceId         = newFace.sourceId,
                semantic         = old.semantic,
                isManualOverride = old.isManualOverride,
                confidence       = newConfidence,
                geometrySignature = newFace.geometrySignature
            };
        }

        // ---- 候補検索 ----

        private static List<FaceSemanticRecord> FindExactCandidates(
            IReadOnlyList<FaceSemanticRecord> oldRecords,
            GeometrySignature sig)
        {
            var result = new List<FaceSemanticRecord>();
            foreach (var r in oldRecords)
            {
                if (r.geometrySignature.ExactMatchPrimary(sig))
                    result.Add(r);
            }
            return result;
        }

        private static List<FaceSemanticRecord> FindNearbyCandidates(
            IReadOnlyList<FaceSemanticRecord> oldRecords,
            GeometrySignature sig)
        {
            var result = new List<FaceSemanticRecord>();
            foreach (var r in oldRecords)
            {
                if (r.geometrySignature.NearbyMatchPrimary(sig, 1))
                    result.Add(r);
            }
            return result;
        }

        private static List<FaceSemanticRecord> FilterByBoundsSize(
            List<FaceSemanticRecord> candidates,
            GeometrySignature sig)
        {
            var result = new List<FaceSemanticRecord>();
            foreach (var r in candidates)
            {
                if (r.geometrySignature.BoundsSizeNearby(sig, 1))
                    result.Add(r);
            }
            return result;
        }

        // ---- スコアリング ----

        private struct ScoredRecord
        {
            public FaceSemanticRecord record;
            public float score;   // 0 に近いほど良い
        }

        private static List<ScoredRecord> ScoreAndFilter(
            List<FaceSemanticRecord> candidates,
            AnalyzedFace newFace)
        {
            var result = new List<ScoredRecord>();

            // 旧 record の center・normal・area を GeometrySignature から復元するのは精度損失があるため、
            // GeometrySignature のビン値から量子化単位を掛け戻す近似値を使う。
            // ただし仕様では旧記録に生の数値がないため、量子化値から逆算する。
            // （閾値チェックは量子化誤差を含む近似判定になる）

            foreach (var r in candidates)
            {
                // center distance（量子化ビン → 元スケール近似）
                var approxOldCenter = new Vector3(
                    r.geometrySignature.quantizedCenter.x * 0.05f,
                    r.geometrySignature.quantizedCenter.y * 0.05f,
                    r.geometrySignature.quantizedCenter.z * 0.05f);
                float dist = Vector3.Distance(approxOldCenter, newFace.faceCenter);
                if (dist > MaxCenterDistance) continue;

                // normal angle
                var approxOldNormal = new Vector3(
                    r.geometrySignature.quantizedNormal.x * 0.1f,
                    r.geometrySignature.quantizedNormal.y * 0.1f,
                    r.geometrySignature.quantizedNormal.z * 0.1f).normalized;
                float angleDeg = Vector3.Angle(approxOldNormal, newFace.faceNormal);
                if (angleDeg > MaxNormalAngleDeg) continue;

                // area relative diff
                float approxOldArea = r.geometrySignature.quantizedArea * 0.05f;
                float areaRelDiff = approxOldArea > 0f
                    ? Mathf.Abs(approxOldArea - newFace.faceArea) / approxOldArea
                    : 1f;
                if (areaRelDiff > MaxAreaRelativeDiff) continue;

                // 複合スコア（仕様書 9.6, 9.7）
                float centerScore = Mathf.Clamp01(dist / MaxCenterDistance);
                float normalScore = Mathf.Clamp01(angleDeg / MaxNormalAngleDeg);
                float areaScore   = Mathf.Clamp01(areaRelDiff / MaxAreaRelativeDiff);
                float total = WeightCenter * centerScore
                            + WeightNormal * normalScore
                            + WeightArea   * areaScore;

                result.Add(new ScoredRecord { record = r, score = total });
            }

            return result;
        }

        // ---- 失敗時のフォールバック（仕様書 9.9）----

        private static FaceSemanticRecord FailRecord(AnalyzedFace newFace)
        {
            return new FaceSemanticRecord
            {
                sourceId          = newFace.sourceId,
                semantic          = newFace.semantic,   // 自動分類結果
                isManualOverride  = false,
                confidence        = 0.5f,
                geometrySignature = newFace.geometrySignature
            };
        }
    }
}
