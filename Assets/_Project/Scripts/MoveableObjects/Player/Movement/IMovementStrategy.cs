using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    public interface IMovementStrategy
    {
        Vector3 ComputeVelocity(CharacterStateContext context, float deltaTime);
    }
}
