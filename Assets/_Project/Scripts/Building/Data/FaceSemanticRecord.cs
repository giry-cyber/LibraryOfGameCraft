using System;

namespace LibraryOfGamecraft.Building
{
    [Serializable]
    public class FaceSemanticRecord
    {
        public int sourceId;
        public FaceSemantic semantic;
        public bool isManualOverride;
        public float confidence;
        public GeometrySignature geometrySignature;
    }
}
