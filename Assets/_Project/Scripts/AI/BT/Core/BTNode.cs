using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    public abstract class BTNode : ScriptableObject
    {
        [SerializeField, HideInInspector] private Vector2 _editorPosition;

        public BTStatus Tick(BTContext ctx)
        {
            var status = Execute(ctx);
#if UNITY_EDITOR
            EditorLastStatus    = status;
            EditorLastTickFrame = UnityEngine.Time.frameCount;
#endif
            return status;
        }

        protected abstract BTStatus Execute(BTContext ctx);

#if UNITY_EDITOR
        public BTStatus EditorLastStatus    { get; private set; }
        public int      EditorLastTickFrame { get; private set; }

        public Vector2 EditorPosition
        {
            get => _editorPosition;
            set { _editorPosition = value; UnityEditor.EditorUtility.SetDirty(this); }
        }
#endif
    }
}
