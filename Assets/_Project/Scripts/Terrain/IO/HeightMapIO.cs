using System.IO;
using UnityEngine;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// ハイトマップ（float配列）のバイナリ読み書きを担当する静的ユーティリティ。
    /// float32 リトルエンディアン形式で保存する。
    /// </summary>
    public static class HeightMapIO
    {
        /// <summary>
        /// float 配列を float32 バイナリとしてアセットパスへ保存する。
        /// </summary>
        public static void Save(float[] data, string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            byte[] bytes = new byte[data.Length * sizeof(float)];
            System.Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
            File.WriteAllBytes(fullPath, bytes);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        /// <summary>
        /// バイナリファイルを読み込んで float 配列として返す。ファイルが存在しない場合は null を返す。
        /// </summary>
        public static float[] Load(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
                return null;

            byte[] bytes = File.ReadAllBytes(fullPath);
            float[] data = new float[bytes.Length / sizeof(float)];
            System.Buffer.BlockCopy(bytes, 0, data, 0, bytes.Length);
            return data;
        }
    }
}
