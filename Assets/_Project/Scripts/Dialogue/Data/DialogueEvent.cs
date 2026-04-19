using System;

namespace LibraryOfGamecraft.Dialogue
{
    public enum SkipEventPolicy
    {
        MustRunOnSkip,
        OptionalOnSkip,
        VisualOnlyOnSkip
    }

    public enum EventWaitMode
    {
        Immediate,
        WaitForComplete,
        WaitForSignal
    }

    [Serializable]
    public class DialogueEvent
    {
        public string EventId;
        public string[] Parameters;
        public SkipEventPolicy SkipPolicy;
        public EventWaitMode WaitMode;
    }
}
