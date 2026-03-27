namespace LibraryOfGamecraft.Player
{
    public class AttackCommand : IPlayerCommand
    {
        public void Execute(CharacterStateContext context)
        {
            context.StateMachine.RequestTransition(CharacterStateType.Attack);
        }
    }
}
