namespace LibraryOfGamecraft.BT
{
    // Action ノードの基底。Running を返せる処理を書く。
    // OnEnter: 初めて Tick されたとき（前フレームは Running でなかった）に呼ばれる。
    // OnTick:  毎 Tick 呼ばれる。Success / Failure / Running を返す。
    // OnExit:  Success または Failure を返したとき（処理が終わったとき）に呼ばれる。
    public abstract class BTAction : BTNode
    {
        private const int Inactive = int.MinValue;
        private string ActiveFrameKey => $"__act_{GetInstanceID()}";

        protected override BTStatus Execute(BTContext ctx)
        {
            int currentFrame = UnityEngine.Time.frameCount;
            int lastFrame    = ctx.Blackboard.Get<int>(ActiveFrameKey, Inactive);
            // 直前フレームまたは同フレームなら継続、それ以外（中断後の再開含む）は再入
            bool continuing = lastFrame != Inactive && (currentFrame - lastFrame) <= 1;

            if (!continuing)
                OnEnter(ctx);

            ctx.Blackboard.Set(ActiveFrameKey, currentFrame);

            var status = OnTick(ctx);

            if (status != BTStatus.Running)
            {
                ctx.Blackboard.Set(ActiveFrameKey, Inactive);
                OnExit(ctx, status);
            }

            return status;
        }

        // 親コンポジットに中断されたとき OnExit を強制発火する
        public override void ForceExit(BTContext ctx)
        {
            int lastFrame = ctx.Blackboard.Get<int>(ActiveFrameKey, Inactive);
            if (lastFrame == Inactive) return;
            ctx.Blackboard.Set(ActiveFrameKey, Inactive);
            OnExit(ctx, BTStatus.Failure);
        }

        protected virtual void OnEnter(BTContext ctx) { }
        protected abstract BTStatus OnTick(BTContext ctx);
        protected virtual void OnExit(BTContext ctx, BTStatus status) { }
    }
}
