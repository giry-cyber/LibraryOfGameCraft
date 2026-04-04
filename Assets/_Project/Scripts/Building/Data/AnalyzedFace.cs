using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    /// <summary>
    /// ProBuilder フェイス解析の実行時データ（シリアライズしない）
    /// </summary>
    public class AnalyzedFace
    {
        public int sourceId;
        public Vector3 faceCenter;   // ワールド空間
        public Vector3 faceNormal;   // ワールド空間・正規化済み
        public float faceArea;
        public Bounds bounds;        // ワールド空間
        public List<Vector3> worldVertices = new(); // ワールド空間頂点（可視化・メッシュ生成用）
        public GeometrySignature geometrySignature;
        public FaceSemantic semantic;
        public float confidence;
    }
}
