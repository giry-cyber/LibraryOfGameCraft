using UnityEngine;

/// <summary>
/// グリッド座標から Scene 名を生成するリゾルバ。
/// 命名規則: World_{x}_{z}（例: World_0_0、World_-1_2）
/// </summary>
public static class SceneNameResolver
{
    private const string Prefix = "World";

    /// <summary>
    /// グリッド座標から Scene 名を生成する。
    /// 例: (2, -1) → "World_2_-1"
    /// </summary>
    public static string Resolve(Vector2Int grid)
    {
        // string.Format より StringBuilder の方が GC に優しいが、
        // ここは毎フレーム呼ばれる箇所ではないため interpolation で十分。
        return $"{Prefix}_{grid.x}_{grid.y}";
    }

    /// <summary>
    /// Scene 名からグリッド座標を逆パースする。
    /// パースできない場合は false を返す。
    /// </summary>
    public static bool TryParse(string sceneName, out Vector2Int grid)
    {
        grid = Vector2Int.zero;
        if (string.IsNullOrEmpty(sceneName)) return false;

        // "World_2_-1" → ["World", "2", "-1"]
        // ただし負数のアンダースコア区切りに注意: Split は余分な区間を作る可能性がある
        // 安全のため Prefix_ を先頭で除去してから残りを最後の _ で分割する
        string expectedPrefix = Prefix + "_";
        if (!sceneName.StartsWith(expectedPrefix)) return false;

        string remainder = sceneName.Substring(expectedPrefix.Length); // "2_-1"
        int lastUnderscore = remainder.LastIndexOf('_');
        if (lastUnderscore < 0) return false;

        string xStr = remainder.Substring(0, lastUnderscore);
        string zStr = remainder.Substring(lastUnderscore + 1);

        if (int.TryParse(xStr, out int x) && int.TryParse(zStr, out int z))
        {
            grid = new Vector2Int(x, z);
            return true;
        }

        return false;
    }
}
