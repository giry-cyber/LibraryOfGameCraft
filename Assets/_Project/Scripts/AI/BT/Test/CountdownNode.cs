using UnityEngine;

namespace LibraryOfGamecraft.BT.Test
{
    // N フレーム Running を返した後に Success を返す。
    // Running の伝播をテストするためのノード。
    [CreateAssetMenu(fileName = "Countdown", menuName = "LibraryOfGamecraft/BT/Test/Countdown")]
    public class CountdownNode : BTNode
    {
        [SerializeField] private int _frames = 3;

        private string CountKey => $"__countdown_{GetInstanceID()}";

        public static CountdownNode Create(int frames)
        {
            var node = CreateInstance<CountdownNode>();
            node._frames = frames;
            return node;
        }

        public override BTStatus Tick(BTContext ctx)
        {
            var count = ctx.Blackboard.Get<int>(CountKey, 0) + 1;
            ctx.Blackboard.Set(CountKey, count);

            if (count >= _frames)
            {
                ctx.Blackboard.Set(CountKey, 0);
                Debug.Log($"[CountdownNode] {ctx.Owner.name}: {_frames} フレーム経過 → Success");
                return BTStatus.Success;
            }

            Debug.Log($"[CountdownNode] {ctx.Owner.name}: {count}/{_frames} → Running");
            return BTStatus.Running;
        }
    }
}
