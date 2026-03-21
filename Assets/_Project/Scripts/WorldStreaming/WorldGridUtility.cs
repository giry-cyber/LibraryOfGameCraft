using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ワールド座標 ↔ グリッド座標の変換ユーティリティ。
/// グリッドは X-Z 平面に展開し、Y は無視する。
/// </summary>
public static class WorldGridUtility
{
    /// <summary>
    /// ワールド座標からグリッド座標に変換する。
    /// (x=511, z=0) → grid(0,0)、(x=512, z=0) → grid(1,0)
    /// </summary>
    public static Vector2Int WorldToGrid(Vector3 worldPos, float gridSize)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / gridSize),
            Mathf.FloorToInt(worldPos.z / gridSize)
        );
    }

    /// <summary>
    /// グリッド座標からワールド原点（グリッドの左手前角）を返す。
    /// Y は常に 0 とする。Terrain の配置基準に使う。
    /// </summary>
    public static Vector3 GridToWorldOrigin(Vector2Int grid, float gridSize)
    {
        return new Vector3(grid.x * gridSize, 0f, grid.y * gridSize);
    }

    /// <summary>
    /// 中心グリッドを含む、radius マス分の正方形範囲内のグリッド一覧を返す。
    /// radius=1 → 3×3 = 9 グリッド
    /// radius=2 → 5×5 = 25 グリッド
    /// </summary>
    public static List<Vector2Int> GetGridsInRadius(Vector2Int center, int radius)
    {
        int side = 2 * radius + 1;
        var list = new List<Vector2Int>(side * side);

        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int z = center.y - radius; z <= center.y + radius; z++)
            {
                list.Add(new Vector2Int(x, z));
            }
        }

        return list;
    }

    /// <summary>
    /// 2 グリッド間のチェビシェフ距離（正方形距離）を返す。
    /// ロード範囲の判定に使える。
    /// </summary>
    public static int ChebyshevDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }
}
