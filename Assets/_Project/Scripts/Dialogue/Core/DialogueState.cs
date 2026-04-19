namespace LibraryOfGamecraft.Dialogue
{
    public enum DialogueState
    {
        Idle,
        Opening,
        ResolvingStartNode,
        EnterNode,
        Typing,
        WaitingForAdvance,
        SelectingChoice,
        EvaluatingBranch,
        ExecutingEvent,
        WaitingExternalSequence,
        AutoAdvancing,
        Skipping,
        Suspended,
        Closing
    }
}
