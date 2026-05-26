using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    public abstract class BTNode : ScriptableObject
    {
        [SerializeField, HideInInspector] private Vector2 _editorPosition;

        public abstract BTStatus Tick(BTContext context);

#if UNITY_EDITOR
        public Vector2 EditorPosition
        {
            get => _editorPosition;
            set { _editorPosition = value; UnityEditor.EditorUtility.SetDirty(this); }
        }
#endif
    }
}
