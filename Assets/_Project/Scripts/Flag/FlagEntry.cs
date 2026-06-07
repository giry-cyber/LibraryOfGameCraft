using System;
using UnityEngine;

namespace LibraryOfGamecraft.Flag
{
    [Serializable]
    public class FlagEntry
    {
        public GameFlag Flag;
        public bool DefaultValue;
        public bool RuntimeValue;
    }
}
