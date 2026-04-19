using System;

namespace LibraryOfGamecraft.Dialogue
{
    [Serializable]
    public class BranchItem
    {
        public DialogueCondition Condition;
        public int Priority;
        public string NextNodeId;
    }

    [Serializable]
    public class BranchNode : DialogueNodeBase
    {
        public BranchItem[] Branches;
        public string DefaultNextNodeId;
    }
}
