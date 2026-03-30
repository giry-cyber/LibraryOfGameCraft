using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// ゲームロジック状態を Animator パラメータへ変換する。
    /// ロジックとアニメーションを分離する責務を持つ。
    ///
    /// ActionState は Int ではなく Trigger を使う。
    /// Int 条件は「常に true」のままになるため Any State が毎フレーム遷移を発火してしまう。
    /// Trigger は消費後に自動リセットされるため、遷移が一度だけ発火することを保証できる。
    ///
    /// Trigger の対応は Dictionary で明示的に管理する。
    /// 配列の添字を使うと enum に値を挿入した際にサイレントなバグが発生するため。
    /// 新しい CharacterStateType を追加したら _triggerMap にも追記すること。
    /// </summary>
    public class PlayerAnimatorAdapter
    {
        private static readonly int GroundedParam      = Animator.StringToHash("Grounded");
        private static readonly int SpeedParam         = Animator.StringToHash("Speed");
        private static readonly int VerticalSpeedParam = Animator.StringToHash("VerticalSpeed");

        private static readonly Dictionary<CharacterStateType, int> TriggerMap =
            new Dictionary<CharacterStateType, int>
            {
                { CharacterStateType.Idle,    Animator.StringToHash("ToIdle")    },
                { CharacterStateType.Move,    Animator.StringToHash("ToMove")    },
                { CharacterStateType.Jump,    Animator.StringToHash("ToJump")    },
                { CharacterStateType.Fall,    Animator.StringToHash("ToFall")    },
                { CharacterStateType.Landing, Animator.StringToHash("ToLanding") },
                { CharacterStateType.Attack,  Animator.StringToHash("ToAttack")  },
                { CharacterStateType.Dodge,   Animator.StringToHash("ToDodge")   },
                { CharacterStateType.Stun,    Animator.StringToHash("ToStun")    },
            };

        private readonly Animator _animator;
        private CharacterStateType _lastStateType = (CharacterStateType)(-1);

        public PlayerAnimatorAdapter(Animator animator)
        {
            _animator = animator;
        }

        public void Update(ICharacterMotor motor, CharacterStateMachine stateMachine, float normalizedSpeed)
        {
            if (_animator == null) return;

            _animator.SetBool(GroundedParam, motor.GroundInfo.IsGrounded);
            _animator.SetFloat(SpeedParam, normalizedSpeed);
            _animator.SetFloat(VerticalSpeedParam, motor.VerticalVelocity);

            // ステートが変化したときだけ対応する Trigger を発火する
            if (stateMachine.CurrentStateType != _lastStateType)
            {
                _lastStateType = stateMachine.CurrentStateType;
                if (TriggerMap.TryGetValue(stateMachine.CurrentStateType, out var trigger))
                    _animator.SetTrigger(trigger);
                else
                    Debug.LogWarning($"[PlayerAnimatorAdapter] TriggerMap に未登録のステート: {stateMachine.CurrentStateType}");
            }
        }
    }
}
