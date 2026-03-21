using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ロード済み Terrain 同士の隣接接続を更新するクラス。
/// Terrain.SetNeighbors を使って、グリッド境界の縫い目（スカート）をなくす。
///
/// 呼び出しタイミング:
///   - Scene のロード完了後
///   - Scene のアンロード完了後
///
/// Terrain が存在しない Scene や、未ロードのグリッドは null として扱い、
/// 落ちないようにしている。
/// </summary>
public class TerrainNeighborUpdater : MonoBehaviour
{
    private WorldSceneStreamer _streamer;
    private float _gridSize;

    // ── 初期化 ────────────────────────────────────────

    public void Initialize(WorldSceneStreamer streamer, float gridSize)
    {
        _streamer = streamer;
        _gridSize = gridSize;
    }

    // ── 公開メソッド ─────────────────────────────────

    /// <summary>
    /// 変化したグリッドを中心に 3×3 範囲の Terrain 接続を更新する。
    /// 未ロードのグリッドは null = 接続なしとして扱う。
    /// </summary>
    public void UpdateNeighbors(Vector2Int changedGrid)
    {
        if (_streamer == null) return;

        // 変化点の周囲 1 マス（計 9 マス）を全て更新する
        // これによって、ロード/アンロードされたグリッドの周辺 Terrain も正しく再接続される
        List<Vector2Int> gridsToUpdate = WorldGridUtility.GetGridsInRadius(changedGrid, 1);

        foreach (Vector2Int grid in gridsToUpdate)
        {
            if (!_streamer.LoadedScenes.TryGetValue(grid, out Scene scene)) continue;

            Terrain terrain = FindTerrainInScene(scene);
            if (terrain == null) continue; // Terrain のない Scene は無視

            // Unity の SetNeighbors: (left, top, right, bottom)
            // Grid 座標の Z+ が「上」、X+ が「右」に対応する
            Terrain left   = GetTerrainAt(new Vector2Int(grid.x - 1, grid.y));
            Terrain top    = GetTerrainAt(new Vector2Int(grid.x,     grid.y + 1));
            Terrain right  = GetTerrainAt(new Vector2Int(grid.x + 1, grid.y));
            Terrain bottom = GetTerrainAt(new Vector2Int(grid.x,     grid.y - 1));

            terrain.SetNeighbors(left, top, right, bottom);
        }
    }

    /// <summary>
    /// Scene 内の最初の Terrain を返す。存在しない場合は null。
    /// </summary>
    public Terrain FindTerrainInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // インアクティブな Terrain は対象外（必要なら true にする）
            Terrain t = root.GetComponentInChildren<Terrain>(false);
            if (t != null) return t;
        }

        return null;
    }

    // ── 内部ヘルパー ─────────────────────────────────

    private Terrain GetTerrainAt(Vector2Int grid)
    {
        if (!_streamer.LoadedScenes.TryGetValue(grid, out Scene scene)) return null;
        return FindTerrainInScene(scene);
    }
}
