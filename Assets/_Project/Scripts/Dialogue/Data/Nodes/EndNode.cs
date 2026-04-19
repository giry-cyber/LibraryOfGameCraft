using System;

namespace LibraryOfGamecraft.Dialogue
{
    [Serializable]
    public class EndNode : DialogueNodeBase
    {
        public string EndReason;
        public DialogueEvent[] EndEvents;
    }
}
