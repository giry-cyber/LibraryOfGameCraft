using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// ジャンプ管理クラス。通常ジャンプと空中追加ジャンプ（2段ジャンプ）を担当する。
    ///
    /// フロー：
    ///   Update       → RequestJump() でジャンプ要求を登録（フラグを立てる）
    ///   FixedUpdate  → TryExecuteJump() で条件確認・Rigidbody に力を加える
    ///
    /// 入力フレームと物理フレームが一致しない場合でも、
    /// フラグ方式のため入力を取りこぼさない。
    /// </summary>
    public class JumpHandler : MonoBehaviour
    {
        private PlayerApiHub _hub;
        private Rigidbody    _rb;

        private bool _jumpRequested;       // RequestJump() でセット、TryExecuteJump() でクリア
        private int  _extraJumpsRemaining; // 空中追加ジャンプの残り回数

        private void Awake()
        {
            _hub = GetComponent<PlayerApiHub>();
        }

        private void Start()
        {
            _rb = _hub.Rigidbody;
            _extraJumpsRemaining = _hub.MaxExtraJumps;
        }

        /// <summary>
        /// Update から呼ぶ。ジャンプ要求をフラグで登録する。
        /// 実際の力の適用は FixedUpdate の TryExecuteJump() で行う。
        /// </summary>
        public void RequestJump()
        {
            _jumpRequested = true;
        }

        /// <summary>
        /// 追加ジャンプ回数を全回復する。着地時に呼ぶ。
        /// </summary>
        public void ResetExtraJumps()
        {
            _extraJumpsRemaining = _hub.MaxExtraJumps;
        }

        /// <summary>
        /// FixedUpdate から呼ぶ。ジャンプを実行できれば true を返す。
        ///   - 接地中  → 通常ジャンプ（回数制限なし）
        ///   - 空中中  → 追加ジャンプ残数を1消費してジャンプ
        ///   - 残数なし → false を返す（何もしない）
        /// </summary>
        public bool TryExecuteJump()
        {
            if (!_jumpRequested) return false;
            _jumpRequested = false;

            if (_hub.GroundChecker.IsGrounded)
            {
                PerformJump();
                return true;
            }

            if (_extraJumpsRemaining > 0)
            {
                _extraJumpsRemaining--;
                PerformJump();
                return true;
            }

            return false;
        }

        private void PerformJump()
        {
            // 垂直速度をゼロにリセットしてから Impulse を加える。
            // これにより落下中の 2段ジャンプでも一定の高さが得られる。
            Vector3 vel = _rb.linearVelocity;
            vel.y = 0f;
            _rb.linearVelocity = vel;

            _rb.AddForce(Vector3.up * _hub.JumpForce, ForceMode.Impulse);
        }
    }
}
