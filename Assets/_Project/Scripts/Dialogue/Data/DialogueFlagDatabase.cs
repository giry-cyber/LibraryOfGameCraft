using System;
using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Dialogue
{
    public enum FlagType
    {
        Bool,
        Int
    }

    [Serializable]
    public class DialogueFlagData
    {
        public string FlagId;
        public FlagType FlagType;
        public bool DefaultBoolValue;
        public int DefaultIntValue;
    }

    [CreateAssetMenu(fileName = "DialogueFlagDatabase", menuName = "LibraryOfGamecraft/Dialogue/FlagDatabase")]
    public class DialogueFlagDatabase : ScriptableObject
    {
        public List<DialogueFlagData> Flags = new List<DialogueFlagData>();
    }
}
