using System;

namespace LibraryOfGamecraft.Dialogue
{
    [Serializable]
    public class DialogueLogEntry
    {
        public int Order;
        public string SpeakerName;
        public string Text;

        public DialogueLogEntry(int order, string speakerName, string text)
        {
            Order = order;
            SpeakerName = speakerName;
            Text = text;
        }
    }
}
