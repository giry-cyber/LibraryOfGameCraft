namespace LibraryOfGamecraft.Player
{
    public class JumpCommand : IPlayerCommand
    {
        public void Execute(CharacterStateContext context)
        {
            context.StateMachine.RequestTransition(CharacterStateType.Jump);
        }
    }
}
