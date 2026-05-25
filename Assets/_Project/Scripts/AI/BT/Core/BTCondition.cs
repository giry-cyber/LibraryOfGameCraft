namespace LibraryOfGamecraft.BT
{
    // Condition ノードの基底。Running を返さず Success / Failure のみ。
    public abstract class BTCondition : BTNode
    {
        public override BTStatus Tick(BTContext ctx) =>
            Check(ctx) ? BTStatus.Success : BTStatus.Failure;

        protected abstract bool Check(BTContext ctx);
    }
}
