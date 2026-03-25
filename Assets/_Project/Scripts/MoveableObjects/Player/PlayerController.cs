using UnityEngine;
using LibraryOfGamecraft.Player.Commands;
using LibraryOfGamecraft.Player.States;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// Player の入口クラス（Facade）。
    ///
    /// 責務：
    ///   ・Update  : 入力をコマンドに変換して実行する
    ///   ・FixedUpdate : ステートマシン経由で物理処理を呼び出す
    ///
    /// 設計上の拡張ポイント：
    ///   ・新しい操作（ダッシュ）→ DashCommand を IPlayerCommand で実装して追加
    ///   ・新しい状態（ダッシュ中）→ DashState を IPlayerState で実装して追加
    ///   ・このクラス自体の変更は最小限（コマンド追加時に if ブロックを1つ追加する程度）
    /// </summary>
    [RequireComponent(typeof(PlayerApiHub))]
    [RequireComponent(typeof(InputReader))]
    [RequireComponent(typeof(MovementMotor))]
    [RequireComponent(typeof(GroundChecker))]
    [RequireComponent(typeof(JumpHandler))]
    [RequireComponent(typeof(PlayerAnimator))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerApiHub       _hub;
        private PlayerStateMachine _stateMachine;

        private void Awake()
        {
            _hub = GetComponent<PlayerApiHub>();
        }

        private void Start()
        {
            // 初期状態は Idle。全コンポーネントが Awake を終えた後に生成する。
            _stateMachine = new PlayerStateMachine(_hub, new IdleState());
            _hub.SetStateMachine(_stateMachine);
        }

        private void Update()
        {
            // ─── コマンド生成・実行（入力 → 行動の変換） ───────────────────────

            // 移動コマンド：入力値を MovementMotor に渡す（力の適用は FixedUpdate）
            IPlayerCommand moveCmd = new MoveCommand(_hub.MovementMotor, _hub.InputReader.MoveInput);
            moveCmd.Execute();

            // ジャンプコマンド：押したフレームのみ登録（JumpHandler がフラグを保持）
            if (_hub.InputReader.JumpPressed)
            {
                IPlayerCommand jumpCmd = new JumpCommand(_hub.JumpHandler);
                jumpCmd.Execute();
                _hub.InputReader.ConsumeJump(); // 二重登録防止
            }

            // ステートマシンの Update（遷移判定など）
            _stateMachine.Update();
        }

        private void FixedUpdate()
        {
            // ステートマシンの FixedUpdate → 現在の状態が Rigidbody 操作を行う
            _stateMachine.FixedUpdate();
        }
    }
}
