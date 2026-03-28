namespace LibraryOfGamecraft.Player
{
    public class DashCommand : IPlayerCommand
    {
        public void Execute(CharacterStateContext context)
        {
            context.StateMachine.RequestTransition(CharacterStateType.Dodge);
        }
    }
}
