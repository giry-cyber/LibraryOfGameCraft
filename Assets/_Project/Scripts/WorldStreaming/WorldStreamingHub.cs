using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 他システムとの連携窓口（ファサード）。
///
/// WorldStreamingController が内部で使う各コンポーネントの情報を、
/// 外部から安全に参照できる形で公開する。
///
/// 使い方の例:
///   var hub = FindFirstObjectByType&lt;WorldStreamingHub&gt;();
///   hub.OnSceneLoaded += (grid, scene) => SpawnEnemies(grid);
///   hub.OnGridChanged += newGrid => ChangeBGM(newGrid);
/// </summary>
public class WorldStreamingHub : MonoBehaviour
{
    // ── 公開プロパティ ───────────────────────────────

    /// <summary>プレイヤーが現在いるグリッド座標</summary>
    public Vector2Int CurrentGrid { get; private set; }

    /// <summary>初期化が完了しているか</summary>
    public bool IsReady { get; private set; }

    // ── イベント ────────────────────────────────────

    /// <summary>プレイヤーが別のグリッドに移動したとき</summary>
    public event Action<Vector2Int> OnGridChanged;

    /// <summary>Scene のロードが完了したとき (グリッド座標, Scene ハンドル)</summary>
    public event Action<Vector2Int, Scene> OnSceneLoaded;

    /// <summary>Scene のアンロードが完了したとき (グリッド座標)</summary>
    public event Action<Vector2Int> OnSceneUnloaded;

    /// <summary>Scene のロードに失敗したとき (グリッド座標, Scene 名)</summary>
    public event Action<Vector2Int, string> OnSceneLoadFailed;

    // ── 内部参照 ─────────────────────────────────────

    private WorldSceneStreamer _streamer;

    // ── 初期化 ────────────────────────────────────────

    /// <summary>WorldStreamingController から呼ばれる初期化処理</summary>
    public void Initialize(WorldSceneStreamer streamer)
    {
        _streamer = streamer;

        // Streamer のイベントをそのまま外部に中継する
        _streamer.OnSceneLoaded    += (grid, scene) => OnSceneLoaded?.Invoke(grid, scene);
        _streamer.OnSceneUnloaded  += grid          => OnSceneUnloaded?.Invoke(grid);
        _streamer.OnSceneLoadFailed += (grid, name) => OnSceneLoadFailed?.Invoke(grid, name);

        IsReady = true;
    }

    // ── 外部公開メソッド ─────────────────────────────

    /// <summary>指定グリッドのシーンがロード済みか</summary>
    public bool IsSceneLoaded(Vector2Int grid)
        => _streamer != null && _streamer.IsLoaded(grid);

    /// <summary>指定グリッドのシーンがロード中か</summary>
    public bool IsSceneLoading(Vector2Int grid)
        => _streamer != null && _streamer.IsLoading(grid);

    /// <summary>
    /// 現在ロード済みの全グリッド座標のスナップショットを返す。
    /// 毎フレーム呼ぶ用途には向かない（List を都度生成するため）。
    /// </summary>
    public List<Vector2Int> GetLoadedGrids()
        => _streamer?.GetLoadedGridsSnapshot() ?? new List<Vector2Int>();

    /// <summary>
    /// 指定グリッドの Scene ハンドルを取得する。
    /// ロード済みでなければ false を返す。
    /// </summary>
    public bool TryGetLoadedScene(Vector2Int grid, out Scene scene)
    {
        if (_streamer != null && _streamer.LoadedScenes.TryGetValue(grid, out scene))
            return true;

        scene = default;
        return false;
    }

    /// <summary>
    /// 指定グリッドの Scene 内にある Terrain を取得する。
    /// Scene が未ロード、または Terrain がない場合は null を返す。
    /// </summary>
    public Terrain GetTerrainAt(Vector2Int grid)
    {
        if (!TryGetLoadedScene(grid, out Scene scene)) return null;

        var updater = GetComponent<TerrainNeighborUpdater>();
        return updater != null ? updater.FindTerrainInScene(scene) : null;
    }

    // ── 内部通知（Controller からのみ呼ぶ） ─────────────

    /// <summary>グリッドが変わったとき Controller から呼ばれる</summary>
    internal void NotifyGridChanged(Vector2Int newGrid)
    {
        CurrentGrid = newGrid;
        OnGridChanged?.Invoke(newGrid);
    }
}
