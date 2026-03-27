using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    public class AirMovementStrategy : IMovementStrategy
    {
        // 空中での水平制御は地上より制限する
        private const float AirControlFactor = 0.4f;

        public Vector3 ComputeVelocity(CharacterStateContext context, float deltaTime)
        {
            var input = context.CurrentInput.Move;
            var tuning = context.MovementTuning;
            var cam = context.CameraTransform;

            Vector3 wishDir;
            if (cam != null)
            {
                var camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                var camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
                wishDir = camForward * input.y + camRight * input.x;
            }
            else
            {
                wishDir = new Vector3(input.x, 0f, input.y);
            }

            float inputMag = wishDir.magnitude;
            if (inputMag > 0.01f) wishDir /= inputMag;

            float targetSpeed = inputMag * tuning.MoveSpeed;
            float accel = tuning.Acceleration * AirControlFactor;
            return Vector3.MoveTowards(context.Motor.HorizontalVelocity, wishDir * targetSpeed, accel * deltaTime);
        }
    }
}
