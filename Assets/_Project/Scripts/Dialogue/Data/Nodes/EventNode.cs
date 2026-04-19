using System;

namespace LibraryOfGamecraft.Dialogue
{
    [Serializable]
    public class EventNode : DialogueNodeBase
    {
        public DialogueEvent[] Events;
        public EventWaitMode WaitMode;
    }
}
