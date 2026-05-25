namespace LibraryOfGamecraft.BT
{
    // Action ノードの基底。Running を返せる処理を書く。
    // OnEnter: 初めて Tick されたとき（前フレームは Running でなかった）に呼ばれる。
    // OnTick:  毎 Tick 呼ばれる。Success / Failure / Running を返す。
    // OnExit:  Success または Failure を返したとき（処理が終わったとき）に呼ばれる。
    public abstract class BTAction : BTNode
    {
        private string ActiveKey => $"__act_{GetInstanceID()}";

        public override BTStatus Tick(BTContext ctx)
        {
            var wasActive = ctx.Blackboard.Get<bool>(ActiveKey, false);
            if (!wasActive)
            {
                OnEnter(ctx);
                ctx.Blackboard.Set(ActiveKey, true);
            }

            var status = OnTick(ctx);

            if (status != BTStatus.Running)
            {
                ctx.Blackboard.Set(ActiveKey, false);
                OnExit(ctx, status);
            }

            return status;
        }

        protected virtual void OnEnter(BTContext ctx) { }
        protected abstract BTStatus OnTick(BTContext ctx);
        protected virtual void OnExit(BTContext ctx, BTStatus status) { }
    }
}
