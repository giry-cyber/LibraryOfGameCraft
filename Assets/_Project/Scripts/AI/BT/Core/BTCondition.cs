namespace LibraryOfGamecraft.BT
{
    // Condition ノードの基底。Running を返さず Success / Failure のみ。
    public abstract class BTCondition : BTNode
    {
        protected override BTStatus Execute(BTContext ctx) =>
            Check(ctx) ? BTStatus.Success : BTStatus.Failure;

        protected abstract bool Check(BTContext ctx);
    }
}
