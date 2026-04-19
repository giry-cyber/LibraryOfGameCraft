using System;

namespace LibraryOfGamecraft.Dialogue
{
    [Serializable]
    public class ChoiceItem
    {
        public string ChoiceId;
        public string ChoiceText;
        public DialogueCondition[] ShowConditions;
        public DialogueCondition[] EnableConditions;
        public DialogueEvent[] OnSelectedEvents;
        public string NextNodeId;
    }

    [Serializable]
    public class ChoiceNode : DialogueNodeBase
    {
        public string PromptText;
        public ChoiceItem[] Choices;
    }
}
