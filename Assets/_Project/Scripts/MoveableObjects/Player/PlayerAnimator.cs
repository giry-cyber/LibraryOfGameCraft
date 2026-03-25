using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// Player の Animator ラッパー。
    /// ステートクラスからアニメーション遷移を呼び出す窓口。
    ///
    /// Animator に必要なパラメータ（Animator Controller 側で作成すること）：
    ///   IsMoving  : bool  — Idle ↔ Move ステートの遷移条件
    ///   MoveSpeed : float — Move ステート内のブレンドツリー値（0=Walk, 1=Run）
    ///
    /// ブレンドツリーの設定例：
    ///   Threshold 0.0 → Walk アニメーション
    ///   Threshold 1.0 → Run アニメーション
    ///   MoveSpeed は現在の水平速度 / 終端速度 で 0〜1 に正規化される。
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        // Animator.StringToHash でキャッシュすることで文字列比較コストを省く
        private static readonly int IsMovingHash  = Animator.StringToHash("IsMoving");
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");

        [Tooltip("Animator を持つ GameObject（子に配置する場合など）。未設定なら自身から取得する。")]
        [SerializeField] private Animator _animator;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_animator == null)
                Debug.LogError("[PlayerAnimator] Animator が見つかりません。Inspector で設定するか、子 GameObject に追加してください。", this);
        }

        /// <summary>
        /// Idle アニメーションへ遷移する。IdleState.Enter() から呼ぶ。
        /// </summary>
        public void PlayIdle()
        {
            if (_animator == null) return;
            _animator.SetBool(IsMovingHash, false);
        }

        /// <summary>
        /// Move アニメーションへ遷移し、ブレンドツリーを更新する。
        /// MoveState.Enter() および MoveState.Update() から呼ぶ。
        /// </summary>
        /// <param name="normalizedSpeed">現在速度を終端速度で正規化した値（0=Walk, 1=Run）</param>
        public void PlayMove(float normalizedSpeed)
        {
            if (_animator == null) return;
            _animator.SetBool(IsMovingHash, true);
            _animator.SetFloat(MoveSpeedHash, Mathf.Clamp01(normalizedSpeed));
        }
    }
}
