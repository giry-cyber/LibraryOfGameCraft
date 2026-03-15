using UnityEngine;

namespace LibraryOfGamecraft.Noise
{
    /// <summary>
    /// フラクタルノイズ生成のパラメータ設定。
    /// ScriptableObject として Assets に保存するか、インスペクタ上で直接使用できる。
    /// </summary>
    [CreateAssetMenu(fileName = "NoiseSettings", menuName = "LibraryOfGamecraft/Noise/NoiseSettings")]
    public class NoiseSettings : ScriptableObject
    {
        [Header("基本設定")]
        [Tooltip("乱数シード。同じシードは同じノイズパターンを再現する。")]
        public int seed = 0;

        [Tooltip("サンプリング座標に掛けるスケール (周波数に相当)。大きいほど細かいノイズ。")]
        [Min(0.0001f)]
        public float frequency = 1f;

        [Tooltip("オクターブ数。増やすほど細かいディテールが加わるが計算コストも増加。")]
        [Range(1, 8)]
        public int octaves = 4;

        [Tooltip("最初のオクターブの振幅。")]
        [Min(0f)]
        public float amplitude = 1f;

        [Tooltip("オクターブごとに振幅に掛ける係数 (0‥1)。小さいほど上位オクターブの影響が弱い。")]
        [Range(0f, 1f)]
        public float persistence = 0.5f;

        [Tooltip("オクターブごとに周波数に掛ける係数 (通常 2)。大きいほど細かいディテールが強調される。")]
        [Min(1f)]
        public float lacunarity = 2f;

        [Header("オフセット")]
        [Tooltip("ノイズ空間上の原点オフセット。パンニングに使用する。")]
        public Vector3 offset = Vector3.zero;

        /// <summary>デフォルト設定のインスタンスを生成する (ScriptableObject を使わない場合に便利)。</summary>
        public static NoiseSettings Default => CreateInstance<NoiseSettings>();
    }
}
