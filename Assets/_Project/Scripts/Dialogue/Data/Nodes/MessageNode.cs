using System;

namespace LibraryOfGamecraft.Dialogue
{
    [Serializable]
    public class MessageNode : DialogueNodeBase
    {
        public string SpeakerId;
        public string SpeakerDisplayName;
        public string Text;
        public string VoiceId;
        public string PortraitId;
        // -1 でデフォルト速度を使用
        public float TextSpeedOverride = -1f;
        public bool AutoAdvanceEnabled;
        public float AutoAdvanceDelay = 2f;
    }
}
