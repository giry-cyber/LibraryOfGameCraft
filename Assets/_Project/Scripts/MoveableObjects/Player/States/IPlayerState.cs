namespace LibraryOfGamecraft.Player.States
{
    /// <summary>
    /// ステートパターンの基底インターフェース。
    /// Idle / Move / Jump / Fall の各状態がこれを実装する。
    /// 将来の Dash / Attack などの状態も同様に追加できる。
    /// </summary>
    public interface IPlayerState
    {
        /// <summary>状態に入るとき1回呼ばれる。初期化処理を書く。</summary>
        void Enter(PlayerApiHub hub);

        /// <summary>毎フレーム呼ばれる（Update）。状態遷移の判定などを書く。</summary>
        void Update(PlayerApiHub hub);

        /// <summary>物理フレームごとに呼ばれる（FixedUpdate）。Rigidbody 操作を書く。</summary>
        void FixedUpdate(PlayerApiHub hub);

        /// <summary>状態から出るとき1回呼ばれる。後処理を書く。</summary>
        void Exit(PlayerApiHub hub);
    }
}
