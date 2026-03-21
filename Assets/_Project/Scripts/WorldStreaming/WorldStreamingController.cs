using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// オープンワールド Additive Load システムのエントリーポイント。
///
/// 担当:
///   - プレイヤー位置を毎フレーム監視し、グリッド変更を検知
///   - 必要シーン集合を計算し、差分ロード / アンロードを起動
///   - 各コンポーネントの初期化と接続
///
/// このスクリプトを GameObject に追加すると、
/// WorldSceneStreamer / TerrainNeighborUpdater / WorldStreamingHub が自動追加される。
///
/// Inspector 設定:
///   playerTransform … プレイヤーの Transform（必須）
///   gridSize        … グリッド 1 辺のサイズ（デフォルト 512）
///   loadRadius      … ロードする半径（1 = 3×3 範囲）
///   unloadBuffer    … ロード半径に加えるアンロード猶予（1 推奨）
///                      プレイヤーが境界を往復しても暴れにくくなる
/// </summary>
[RequireComponent(typeof(WorldSceneStreamer))]
[RequireComponent(typeof(TerrainNeighborUpdater))]
[RequireComponent(typeof(WorldStreamingHub))]
public class WorldStreamingController : MonoBehaviour
{
    // ── Inspector 設定 ────────────────────────────────

    [Header("Player")]
    [SerializeField] private Transform playerTransform;

    [Header("Grid Settings")]
    [SerializeField] private float gridSize = 512f;

    [Tooltip("プレイヤー周囲にロードする半径 (1 = 3×3)")]
    [SerializeField] private int loadRadius = 1;

    [Tooltip("アンロード猶予。loadRadius + unloadBuffer を超えた時だけアンロードする。" +
             "境界往復時のチラツキ防止に有効。")]
    [SerializeField] private int unloadBuffer = 1;

    [Header("Startup")]
    [Tooltip("true にすると Start() 時点のプレイヤー位置を基準にシーンをロードする")]
    [SerializeField] private bool loadOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;

    // ── 公開プロパティ ───────────────────────────────

    /// <summary>他システムとの連携窓口</summary>
    public WorldStreamingHub Hub => _hub;

    // ── 内部コンポーネント ────────────────────────────

    private WorldSceneStreamer       _streamer;
    private TerrainNeighborUpdater   _terrainUpdater;
    private WorldStreamingHub        _hub;

    // ── 状態 ─────────────────────────────────────────

    private Vector2Int _currentGrid;
    private bool       _isInitialized;

    // ── Unity ライフサイクル ──────────────────────────

    private void Awake()
    {
        _streamer       = GetComponent<WorldSceneStreamer>();
        _terrainUpdater = GetComponent<TerrainNeighborUpdater>();
        _hub            = GetComponent<WorldStreamingHub>();

        // 各コンポーネントを初期化
        _terrainUpdater.Initialize(_streamer, gridSize);
        _hub.Initialize(_streamer);

        // シーンロード/アンロード完了時にコールバックを登録
        _streamer.OnSceneLoaded   += HandleSceneLoaded;
        _streamer.OnSceneUnloaded += HandleSceneUnloaded;
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("[WorldStreaming] playerTransform が設定されていません。" +
                           "Inspector で Player の Transform をアサインしてください。");
            enabled = false;
            return;
        }

        _currentGrid   = WorldGridUtility.WorldToGrid(playerTransform.position, gridSize);
        _isInitialized = true;

        _hub.NotifyGridChanged(_currentGrid);

        if (loadOnStart)
        {
            if (enableDebugLog)
                Debug.Log($"[WorldStreaming] 初期グリッド: {_currentGrid} → ロード開始");
            UpdateStreaming(_currentGrid);
        }
    }

    private void Update()
    {
        if (!_isInitialized) return;

        Vector2Int newGrid = WorldGridUtility.WorldToGrid(playerTransform.position, gridSize);
        if (newGrid == _currentGrid) return;

        // グリッドが変わった時だけ差分更新（毎フレームスキャンしない）
        if (enableDebugLog)
            Debug.Log($"[WorldStreaming] グリッド変更: {_currentGrid} → {newGrid}");

        _currentGrid = newGrid;
        _hub.NotifyGridChanged(_currentGrid);
        UpdateStreaming(_currentGrid);
    }

    private void OnDestroy()
    {
        if (_streamer == null) return;
        _streamer.OnSceneLoaded   -= HandleSceneLoaded;
        _streamer.OnSceneUnloaded -= HandleSceneUnloaded;
    }

    // ── ストリーミング更新 ────────────────────────────

    /// <summary>
    /// 現在グリッドを中心に、必要なシーンをロード・不要なシーンをアンロードする。
    ///
    /// ロード範囲:   loadRadius              (例: 1 → 3×3)
    /// アンロード範囲: loadRadius + unloadBuffer (例: 2 → 5×5)
    ///
    /// 内側の範囲 = ロードする
    /// 外側の範囲（2 重目）を超えたら = アンロードする
    /// ↑ この 2 重構造が境界往復時の安定化の核心
    /// </summary>
    private void UpdateStreaming(Vector2Int centerGrid)
    {
        // ① ロードすべきグリッド集合
        var requiredGrids = new HashSet<Vector2Int>(
            WorldGridUtility.GetGridsInRadius(centerGrid, loadRadius)
        );

        // ② アンロードしない（キープする）グリッド集合
        //    = loadRadius + unloadBuffer の外に出るまでアンロードしない
        var keepGrids = new HashSet<Vector2Int>(
            WorldGridUtility.GetGridsInRadius(centerGrid, loadRadius + unloadBuffer)
        );

        // ③ ロードリクエスト
        foreach (Vector2Int grid in requiredGrids)
        {
            if (_streamer.IsLoadedOrLoading(grid)) continue;

            string sceneName = SceneNameResolver.Resolve(grid);
            _streamer.RequestLoad(grid, sceneName, enableDebugLog);
        }

        // ④ アンロードリクエスト
        //    GetLoadedGridsSnapshot() で安全なコピーを使う（イテレーション中の変更を避けるため）
        List<Vector2Int> loadedSnapshot = _streamer.GetLoadedGridsSnapshot();
        foreach (Vector2Int grid in loadedSnapshot)
        {
            if (keepGrids.Contains(grid))      continue; // まだキープ範囲内
            if (_streamer.IsUnloading(grid))   continue; // すでにアンロード中

            string sceneName = SceneNameResolver.Resolve(grid);
            _streamer.RequestUnload(grid, sceneName, enableDebugLog);
        }
    }

    // ── イベントハンドラ ─────────────────────────────

    private void HandleSceneLoaded(Vector2Int grid, Scene scene)
    {
        // Terrain 接続を更新
        _terrainUpdater.UpdateNeighbors(grid);

        // ロード中にプレイヤーが遠ざかっていた場合、ロード完了後に即アンロードする
        CheckAndUnloadIfStale(grid);
    }

    private void HandleSceneUnloaded(Vector2Int grid)
    {
        // 隣接 Terrain の接続をクリアし、null に差し替える
        _terrainUpdater.UpdateNeighbors(grid);
    }

    /// <summary>
    /// ロード完了時点でそのグリッドがキープ範囲外になっていれば即アンロードする。
    /// 高速移動時に「ロード中に範囲外になる」エッジケースへの対処。
    /// </summary>
    private void CheckAndUnloadIfStale(Vector2Int grid)
    {
        if (!_isInitialized) return;

        int dist = WorldGridUtility.ChebyshevDistance(grid, _currentGrid);
        if (dist <= loadRadius + unloadBuffer) return;

        string sceneName = SceneNameResolver.Resolve(grid);
        if (enableDebugLog)
            Debug.Log($"[WorldStreaming] ロード完了後に範囲外と判定 → 即アンロード: {sceneName}");

        _streamer.RequestUnload(grid, sceneName, enableDebugLog);
    }

    // ── エディタ向け Gizmo ────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || playerTransform == null) return;

        // ロード範囲（緑の枠）
        DrawGridRangeGizmo(_currentGrid, loadRadius, new Color(0f, 1f, 0f, 0.25f));

        // アンロード余白を含む範囲（黄の枠）
        DrawGridRangeGizmo(_currentGrid, loadRadius + unloadBuffer, new Color(1f, 1f, 0f, 0.1f));
    }

    private void DrawGridRangeGizmo(Vector2Int center, int radius, Color color)
    {
        Gizmos.color = color;
        float totalSize = gridSize * (2 * radius + 1);

        // グリッド原点はグリッドの左手前角なので、中心にオフセットを足す
        Vector3 worldCenter = WorldGridUtility.GridToWorldOrigin(center, gridSize)
                              + new Vector3(gridSize * 0.5f, 0f, gridSize * 0.5f);

        Gizmos.DrawCube(worldCenter, new Vector3(totalSize, 1f, totalSize));
        Gizmos.color = new Color(color.r, color.g, color.b, 1f);
        Gizmos.DrawWireCube(worldCenter, new Vector3(totalSize, 1f, totalSize));
    }
#endif
}
