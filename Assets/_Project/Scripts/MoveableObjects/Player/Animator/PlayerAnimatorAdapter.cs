using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// ゲームロジック状態を Animator パラメータへ変換する。
    /// ロジックとアニメーションを分離する責務を持つ。
    /// </summary>
    public class PlayerAnimatorAdapter
    {
        private static readonly int GroundedParam = Animator.StringToHash("Grounded");
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int VerticalSpeedParam = Animator.StringToHash("VerticalSpeed");
        private static readonly int ActionStateParam = Animator.StringToHash("ActionState");

        private readonly Animator _animator;

        public PlayerAnimatorAdapter(Animator animator)
        {
            _animator = animator;
        }

        public void Update(ICharacterMotor motor, CharacterStateMachine stateMachine)
        {
            if (_animator == null) return;

            _animator.SetBool(GroundedParam, motor.GroundInfo.IsGrounded);
            _animator.SetFloat(SpeedParam, motor.HorizontalVelocity.magnitude);
            _animator.SetFloat(VerticalSpeedParam, motor.VerticalVelocity);
            _animator.SetInteger(ActionStateParam, (int)stateMachine.CurrentStateType);
        }
    }
}
