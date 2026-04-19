using System;

namespace LibraryOfGamecraft.Dialogue
{
    [Serializable]
    public class JumpNode : DialogueNodeBase
    {
        // 別セットへジャンプする場合は指定（空の場合は同一セット内）
        public string TargetSetId;
        public string TargetNodeId;
    }
}
