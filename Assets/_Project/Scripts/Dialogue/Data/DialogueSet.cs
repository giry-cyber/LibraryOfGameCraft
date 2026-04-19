using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Dialogue
{
    [CreateAssetMenu(fileName = "DialogueSet", menuName = "LibraryOfGamecraft/Dialogue/DialogueSet")]
    public class DialogueSet : ScriptableObject
    {
        public string DialogueSetId;
        public string DisplayName;
        public int Priority;
        public DialogueCondition[] StartConditions;
        public string StartNodeId;

        [SerializeReference]
        public List<DialogueNodeBase> Nodes = new List<DialogueNodeBase>();

        public SkipPolicy DefaultSkipPolicy = SkipPolicy.Allowed;
        public bool DefaultAutoEnabled;
        public float DefaultAutoDelay = 2f;
    }
}
