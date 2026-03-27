using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    public class GroundMovementStrategy : IMovementStrategy
    {
        public Vector3 ComputeVelocity(CharacterStateContext context, float deltaTime)
        {
            var input = context.CurrentInput.Move;
            var tuning = context.MovementTuning;
            var cam = context.CameraTransform;

            // カメラ基準でワールド方向を計算
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
            float accel = inputMag > 0.01f ? tuning.Acceleration : tuning.Deceleration;
            return Vector3.MoveTowards(context.Motor.HorizontalVelocity, wishDir * targetSpeed, accel * deltaTime);
        }
    }
}
