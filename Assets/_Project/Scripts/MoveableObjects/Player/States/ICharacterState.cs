namespace LibraryOfGamecraft.Player
{
    public interface ICharacterState
    {
        CharacterStateType StateType { get; }
        IMovementStrategy MovementStrategy { get; }
        void Enter(CharacterStateContext context);
        void Exit(CharacterStateContext context);
        void Tick(CharacterStateContext context, float deltaTime);
        bool CanTransitionTo(CharacterStateType next);
    }
}
