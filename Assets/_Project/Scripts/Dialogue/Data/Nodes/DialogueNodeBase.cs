using System;

namespace LibraryOfGamecraft.Dialogue
{
    public enum DialogueNodeType
    {
        Message,
        Choice,
        Branch,
        Event,
        Sequence,
        Jump,
        End
    }

    public enum SkipPolicy
    {
        Inherit,
        Allowed,
        Disallowed
    }

    public enum LogPolicy
    {
        Record,
        Skip
    }

    [Serializable]
    public abstract class DialogueNodeBase
    {
        public string NodeId;
        public DialogueNodeType NodeType;
        public string Comment;
        public DialogueCondition[] Conditions;
        public DialogueEvent[] PreEvents;
        public DialogueEvent[] PostEvents;
        public SkipPolicy SkipPolicy = SkipPolicy.Inherit;
        public LogPolicy LogPolicy = LogPolicy.Record;
        public string NextNodeId;
    }
}
