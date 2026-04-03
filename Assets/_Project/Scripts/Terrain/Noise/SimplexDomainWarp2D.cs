using UnityEngine;
using LibraryOfGamecraft.Noise;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// シンプレックスノイズを2軸でサンプリングしてドメインワープベクトルを生成する。
    /// 2軸目はオフセット座標 (+3.7, +1.3) を加えることで独立したノイズ場を近似する。
    /// </summary>
    public class SimplexDomainWarp2D : IDomainWarp2D
    {
        private readonly int _seed;
        private readonly float _warpScale;
        private readonly float _warpStrength;

        public SimplexDomainWarp2D(int seed, float warpScale, float warpStrength)
        {
            _seed = seed;
            _warpScale = warpScale;
            _warpStrength = warpStrength;
        }

        public Vector2 Warp(float x, float z)
        {
            SimplexNoise.SetSeed(_seed);

            float wx = SimplexNoise.Evaluate(x * _warpScale, z * _warpScale) * _warpStrength;
            float wz = SimplexNoise.Evaluate(x * _warpScale + 3.7f, z * _warpScale + 1.3f) * _warpStrength;
            return new Vector2(wx, wz);
        }
    }
}
