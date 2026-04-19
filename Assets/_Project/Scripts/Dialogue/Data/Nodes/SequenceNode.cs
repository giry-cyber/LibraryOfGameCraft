using System;

namespace LibraryOfGamecraft.Dialogue
{
    [Serializable]
    public class SequenceNode : DialogueNodeBase
    {
        public string SequenceId;
        public bool WaitForCompletion = true;
        public bool AllowSkip;
    }
}
