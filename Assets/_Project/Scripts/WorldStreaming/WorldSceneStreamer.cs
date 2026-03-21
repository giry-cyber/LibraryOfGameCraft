using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene の非同期ロード / アンロードを管理するクラス。
///
/// 状態管理:
///   _loadedScenes   … グリッド → ロード済み Scene ハンドル
///   _loadingScenes  … グリッド → ロード中 AsyncOperation
///   _unloadingScenes … アンロード中グリッドの集合
///
/// 呼び出し側は RequestLoad / RequestUnload を呼ぶだけでよい。
/// 重複リクエストはここで弾く。
/// </summary>
public class WorldSceneStreamer : MonoBehaviour
{
    // ── イベント ────────────────────────────────────

    /// <summary>Scene のロードが完了したとき。(グリッド座標, Scene ハンドル)</summary>
    public event Action<Vector2Int, Scene> OnSceneLoaded;

    /// <summary>Scene のアンロードが完了したとき。(グリッド座標)</summary>
    public event Action<Vector2Int> OnSceneUnloaded;

    /// <summary>Scene のロードが失敗したとき。(グリッド座標, Scene 名)</summary>
    public event Action<Vector2Int, string> OnSceneLoadFailed;

    // ── 状態 ─────────────────────────────────────────

    /// <summary>ロード済みシーン: グリッド → Scene ハンドル</summary>
    private readonly Dictionary<Vector2Int, Scene> _loadedScenes
        = new Dictionary<Vector2Int, Scene>();

    /// <summary>ロード中シーン: グリッド → AsyncOperation</summary>
    private readonly Dictionary<Vector2Int, AsyncOperation> _loadingScenes
        = new Dictionary<Vector2Int, AsyncOperation>();

    /// <summary>アンロード中グリッドの集合</summary>
    private readonly HashSet<Vector2Int> _unloadingScenes
        = new HashSet<Vector2Int>();

    // ── 公開プロパティ ───────────────────────────────

    /// <summary>ロード済みシーンの読み取り専用ビュー</summary>
    public IReadOnlyDictionary<Vector2Int, Scene> LoadedScenes => _loadedScenes;

    public bool IsLoaded(Vector2Int grid)          => _loadedScenes.ContainsKey(grid);
    public bool IsLoading(Vector2Int grid)         => _loadingScenes.ContainsKey(grid);
    public bool IsUnloading(Vector2Int grid)       => _unloadingScenes.Contains(grid);
    public bool IsLoadedOrLoading(Vector2Int grid) => IsLoaded(grid) || IsLoading(grid);

    /// <summary>
    /// ロード済みグリッドのスナップショットを返す。
    /// イテレーション中にコレクションが変わらないよう安全な複製を返す。
    /// </summary>
    public List<Vector2Int> GetLoadedGridsSnapshot()
    {
        return new List<Vector2Int>(_loadedScenes.Keys);
    }

    // ── 公開メソッド ─────────────────────────────────

    /// <summary>
    /// 指定グリッドのシーンのロードをリクエストする。
    /// すでにロード済み・ロード中の場合は無視する。
    /// </summary>
    public void RequestLoad(Vector2Int grid, string sceneName, bool debugLog)
    {
        if (IsLoaded(grid))
        {
            if (debugLog) Debug.Log($"[Streamer] Skip load (already loaded): {sceneName}");
            return;
        }

        if (IsLoading(grid))
        {
            if (debugLog) Debug.Log($"[Streamer] Skip load (already loading): {sceneName}");
            return;
        }

        StartCoroutine(LoadSceneRoutine(grid, sceneName, debugLog));
    }

    /// <summary>
    /// 指定グリッドのシーンのアンロードをリクエストする。
    /// ロードされていない・アンロード中の場合は無視する。
    /// </summary>
    public void RequestUnload(Vector2Int grid, string sceneName, bool debugLog)
    {
        if (!IsLoaded(grid))
        {
            if (debugLog) Debug.Log($"[Streamer] Skip unload (not loaded): {sceneName}");
            return;
        }

        if (IsUnloading(grid))
        {
            if (debugLog) Debug.Log($"[Streamer] Skip unload (already unloading): {sceneName}");
            return;
        }

        StartCoroutine(UnloadSceneRoutine(grid, sceneName, debugLog));
    }

    // ── コルーチン ───────────────────────────────────

    private IEnumerator LoadSceneRoutine(Vector2Int grid, string sceneName, bool debugLog)
    {
        if (debugLog) Debug.Log($"[Streamer] Loading: {sceneName} (grid {grid})");

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (op == null)
        {
            // Build Settings に Scene が登録されていない場合など
            Debug.LogWarning($"[Streamer] Load failed — Scene not found or not in Build Settings: {sceneName}");
            OnSceneLoadFailed?.Invoke(grid, sceneName);
            yield break;
        }

        _loadingScenes[grid] = op;

        yield return op; // ロード完了まで待つ

        _loadingScenes.Remove(grid);

        // ロード完了後に Scene ハンドルを取得
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning($"[Streamer] Scene loaded but handle is invalid: {sceneName}");
            OnSceneLoadFailed?.Invoke(grid, sceneName);
            yield break;
        }

        _loadedScenes[grid] = scene;

        if (debugLog) Debug.Log($"[Streamer] Loaded: {sceneName} (grid {grid})");
        OnSceneLoaded?.Invoke(grid, scene);
    }

    private IEnumerator UnloadSceneRoutine(Vector2Int grid, string sceneName, bool debugLog)
    {
        if (debugLog) Debug.Log($"[Streamer] Unloading: {sceneName} (grid {grid})");

        _unloadingScenes.Add(grid);

        // Scene ハンドルを使ったアンロードは Scene 名によるあいまい一致を避けられる
        if (!_loadedScenes.TryGetValue(grid, out Scene scene))
        {
            Debug.LogWarning($"[Streamer] Unload requested but scene handle not found: {sceneName}");
            _unloadingScenes.Remove(grid);
            yield break;
        }

        AsyncOperation op = SceneManager.UnloadSceneAsync(scene);

        if (op == null)
        {
            Debug.LogWarning($"[Streamer] UnloadSceneAsync returned null: {sceneName}");
            _unloadingScenes.Remove(grid);
            yield break;
        }

        yield return op; // アンロード完了まで待つ

        _loadedScenes.Remove(grid);
        _unloadingScenes.Remove(grid);

        if (debugLog) Debug.Log($"[Streamer] Unloaded: {sceneName} (grid {grid})");
        OnSceneUnloaded?.Invoke(grid);
    }
}
