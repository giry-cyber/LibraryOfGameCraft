using UnityEngine;
using UnityEngine.Events;

namespace LibraryOfGamecraft.BT
{
    // 攻撃処理の委譲先。AttackAction がこのコンポーネントを呼び出す。
    // ダメージ計算・アニメーションなど実際の処理は OnAttackTriggered に Inspector から配線する。
    public class AttackCapability : MonoBehaviour
    {
        public UnityEvent OnAttackTriggered;

        public void TriggerAttack() => OnAttackTriggered?.Invoke();
    }
}
