using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    /// <summary>
    /// 面ごとの Semantic データを Scene にシリアライズして保持する。
    /// 手動修正を含む全記録の永続化を担当する。
    /// </summary>
    public class SemanticStore : MonoBehaviour
    {
        [SerializeField] private List<FaceSemanticRecord> _records = new();

        public IReadOnlyList<FaceSemanticRecord> Records => _records;

        public void SetRecords(IEnumerable<FaceSemanticRecord> records)
        {
            _records.Clear();
            _records.AddRange(records);
        }

        /// <summary>手動修正: 指定 sourceId の Semantic を上書きする</summary>
        public void ApplyManualOverride(int sourceId, FaceSemantic semantic)
        {
            var record = _records.Find(r => r.sourceId == sourceId);
            if (record == null) return;
            record.semantic          = semantic;
            record.isManualOverride  = true;
            record.confidence        = 1.0f;
        }

        public FaceSemanticRecord FindById(int sourceId)
            => _records.Find(r => r.sourceId == sourceId);
    }
}
