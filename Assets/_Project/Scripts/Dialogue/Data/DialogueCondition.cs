using System;

namespace LibraryOfGamecraft.Dialogue
{
    public enum ConditionTargetType
    {
        FlagBool,
        FlagInt,
        Quest,
        Item,
        DialogueRead,
    }

    public enum ConditionOperator
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }

    [Serializable]
    public class DialogueCondition
    {
        public ConditionTargetType TargetType;
        public string TargetId;
        public ConditionOperator Operator;
        public string Value;
    }
}
