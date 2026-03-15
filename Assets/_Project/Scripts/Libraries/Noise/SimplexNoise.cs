using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LibraryOfGamecraft.Noise
{
    /// <summary>
    /// Ken Perlin のシンプレックスノイズ実装 (2D / 3D / 4D)
    /// 戻り値はすべて [-1, 1] の範囲。
    /// </summary>
    public static class SimplexNoise
    {
        // ---- Permutation table ------------------------------------------------

        private static readonly byte[] Perm;
        private static readonly byte[] PermMod12;

        static SimplexNoise()
        {
            Perm      = new byte[512];
            PermMod12 = new byte[512];

            // デフォルトのシード値で初期化
            InitializePermutation(0);
        }

        /// <summary>
        /// パーミュテーションテーブルをシードで初期化する。
        /// スレッドセーフではないため、アプリ起動時など一度だけ呼ぶこと。
        /// </summary>
        public static void SetSeed(int seed)
        {
            InitializePermutation(seed);
        }

        private static void InitializePermutation(int seed)
        {
            // 0‥255 をフィッシャー‐イェーツシャッフル
            var source = new byte[256];
            for (int i = 0; i < 256; i++) source[i] = (byte)i;

            var rng = new System.Random(seed);
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (source[i], source[j]) = (source[j], source[i]);
            }

            for (int i = 0; i < 512; i++)
            {
                Perm[i]      = source[i & 255];
                PermMod12[i] = (byte)(Perm[i] % 12);
            }
        }

        // ---- Gradient tables --------------------------------------------------

        // 3D グラジエント (12方向)
        private static readonly int[,] Grad3 =
        {
            { 1, 1, 0}, {-1, 1, 0}, { 1,-1, 0}, {-1,-1, 0},
            { 1, 0, 1}, {-1, 0, 1}, { 1, 0,-1}, {-1, 0,-1},
            { 0, 1, 1}, { 0,-1, 1}, { 0, 1,-1}, { 0,-1,-1}
        };

        // 4D グラジェント (32方向)
        private static readonly int[,] Grad4 =
        {
            { 0, 1, 1, 1}, { 0, 1, 1,-1}, { 0, 1,-1, 1}, { 0, 1,-1,-1},
            { 0,-1, 1, 1}, { 0,-1, 1,-1}, { 0,-1,-1, 1}, { 0,-1,-1,-1},
            { 1, 0, 1, 1}, { 1, 0, 1,-1}, { 1, 0,-1, 1}, { 1, 0,-1,-1},
            {-1, 0, 1, 1}, {-1, 0, 1,-1}, {-1, 0,-1, 1}, {-1, 0,-1,-1},
            { 1, 1, 0, 1}, { 1, 1, 0,-1}, { 1,-1, 0, 1}, { 1,-1, 0,-1},
            {-1, 1, 0, 1}, {-1, 1, 0,-1}, {-1,-1, 0, 1}, {-1,-1, 0,-1},
            { 1, 1, 1, 0}, { 1, 1,-1, 0}, { 1,-1, 1, 0}, { 1,-1,-1, 0},
            {-1, 1, 1, 0}, {-1, 1,-1, 0}, {-1,-1, 1, 0}, {-1,-1,-1, 0}
        };

        // ---- Helper -----------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FastFloor(float x) => x > 0 ? (int)x : (int)x - 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Dot2(int gi, float x, float y)
            => Grad3[gi, 0] * x + Grad3[gi, 1] * y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Dot3(int gi, float x, float y, float z)
            => Grad3[gi, 0] * x + Grad3[gi, 1] * y + Grad3[gi, 2] * z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Dot4(int gi, float x, float y, float z, float w)
            => Grad4[gi, 0] * x + Grad4[gi, 1] * y + Grad4[gi, 2] * z + Grad4[gi, 3] * w;

        // ---- 2D ---------------------------------------------------------------

        private const float F2 = 0.366025403f; // (sqrt(3)-1)/2
        private const float G2 = 0.211324865f; // (3-sqrt(3))/6

        /// <summary>2D シンプレックスノイズ。戻り値は [-1, 1]。</summary>
        public static float Evaluate(float x, float y)
        {
            // スキュー変換
            float s  = (x + y) * F2;
            int   i  = FastFloor(x + s);
            int   j  = FastFloor(y + s);
            float t  = (i + j) * G2;

            // 単体の頂点0
            float x0 = x - (i - t);
            float y0 = y - (j - t);

            // 頂点1 (中間頂点)
            int i1, j1;
            if (x0 > y0) { i1 = 1; j1 = 0; }
            else          { i1 = 0; j1 = 1; }

            float x1 = x0 - i1 + G2;
            float y1 = y0 - j1 + G2;
            float x2 = x0 - 1f + 2f * G2;
            float y2 = y0 - 1f + 2f * G2;

            int ii = i & 255;
            int jj = j & 255;

            int gi0 = PermMod12[ii      + Perm[jj     ]];
            int gi1 = PermMod12[ii + i1 + Perm[jj + j1]];
            int gi2 = PermMod12[ii + 1  + Perm[jj + 1 ]];

            float n0 = ContribWeight2(x0, y0, gi0, Dot2);
            float n1 = ContribWeight2(x1, y1, gi1, Dot2);
            float n2 = ContribWeight2(x2, y2, gi2, Dot2);

            return 70f * (n0 + n1 + n2);
        }

        private static float ContribWeight2(float dx, float dy, int gi, Func<int, float, float, float> dot)
        {
            float t = 0.5f - dx * dx - dy * dy;
            if (t < 0f) return 0f;
            t *= t;
            return t * t * dot(gi, dx, dy);
        }

        // ---- 3D ---------------------------------------------------------------

        private const float F3 = 1f / 3f;
        private const float G3 = 1f / 6f;

        /// <summary>3D シンプレックスノイズ。戻り値は [-1, 1]。</summary>
        public static float Evaluate(float x, float y, float z)
        {
            float s = (x + y + z) * F3;
            int   i = FastFloor(x + s);
            int   j = FastFloor(y + s);
            int   k = FastFloor(z + s);
            float t = (i + j + k) * G3;

            float x0 = x - (i - t);
            float y0 = y - (j - t);
            float z0 = z - (k - t);

            int i1, j1, k1, i2, j2, k2;
            if      (x0 >= y0 && y0 >= z0) { i1=1; j1=0; k1=0; i2=1; j2=1; k2=0; }
            else if (x0 >= y0 && x0 >= z0) { i1=1; j1=0; k1=0; i2=1; j2=0; k2=1; }
            else if (x0 >= z0)              { i1=0; j1=1; k1=0; i2=1; j2=1; k2=0; }
            else if (y0 < z0)               { i1=0; j1=0; k1=1; i2=0; j2=1; k2=1; }
            else if (x0 < z0)               { i1=0; j1=1; k1=0; i2=0; j2=1; k2=1; }
            else                            { i1=0; j1=1; k1=0; i2=1; j2=1; k2=0; }

            float x1 = x0 - i1 + G3;
            float y1 = y0 - j1 + G3;
            float z1 = z0 - k1 + G3;
            float x2 = x0 - i2 + 2f * G3;
            float y2 = y0 - j2 + 2f * G3;
            float z2 = z0 - k2 + 2f * G3;
            float x3 = x0 - 1f + 3f * G3;
            float y3 = y0 - 1f + 3f * G3;
            float z3 = z0 - 1f + 3f * G3;

            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;

            int gi0 = PermMod12[ii      + Perm[jj      + Perm[kk     ]]];
            int gi1 = PermMod12[ii + i1 + Perm[jj + j1 + Perm[kk + k1]]];
            int gi2 = PermMod12[ii + i2 + Perm[jj + j2 + Perm[kk + k2]]];
            int gi3 = PermMod12[ii + 1  + Perm[jj + 1  + Perm[kk + 1 ]]];

            float n0 = ContribWeight3(x0, y0, z0, gi0);
            float n1 = ContribWeight3(x1, y1, z1, gi1);
            float n2 = ContribWeight3(x2, y2, z2, gi2);
            float n3 = ContribWeight3(x3, y3, z3, gi3);

            return 32f * (n0 + n1 + n2 + n3);
        }

        private static float ContribWeight3(float dx, float dy, float dz, int gi)
        {
            float t = 0.6f - dx * dx - dy * dy - dz * dz;
            if (t < 0f) return 0f;
            t *= t;
            return t * t * Dot3(gi, dx, dy, dz);
        }

        // ---- 4D ---------------------------------------------------------------

        private const float F4 = 0.309016994f; // (sqrt(5)-1)/4
        private const float G4 = 0.138196601f; // (5-sqrt(5))/20

        /// <summary>4D シンプレックスノイズ。戻り値は [-1, 1]。</summary>
        public static float Evaluate(float x, float y, float z, float w)
        {
            float s = (x + y + z + w) * F4;
            int   i = FastFloor(x + s);
            int   j = FastFloor(y + s);
            int   k = FastFloor(z + s);
            int   l = FastFloor(w + s);
            float t = (i + j + k + l) * G4;

            float x0 = x - (i - t);
            float y0 = y - (j - t);
            float z0 = z - (k - t);
            float w0 = w - (l - t);

            // ランク順で各軸の頂点シフトを決定
            int rankx = 0, ranky = 0, rankz = 0, rankw = 0;
            if (x0 > y0) rankx++; else ranky++;
            if (x0 > z0) rankx++; else rankz++;
            if (x0 > w0) rankx++; else rankw++;
            if (y0 > z0) ranky++; else rankz++;
            if (y0 > w0) ranky++; else rankw++;
            if (z0 > w0) rankz++; else rankw++;

            int i1 = rankx >= 3 ? 1 : 0;
            int j1 = ranky >= 3 ? 1 : 0;
            int k1 = rankz >= 3 ? 1 : 0;
            int l1 = rankw >= 3 ? 1 : 0;
            int i2 = rankx >= 2 ? 1 : 0;
            int j2 = ranky >= 2 ? 1 : 0;
            int k2 = rankz >= 2 ? 1 : 0;
            int l2 = rankw >= 2 ? 1 : 0;
            int i3 = rankx >= 1 ? 1 : 0;
            int j3 = ranky >= 1 ? 1 : 0;
            int k3 = rankz >= 1 ? 1 : 0;
            int l3 = rankw >= 1 ? 1 : 0;

            float x1 = x0 - i1 + G4;
            float y1 = y0 - j1 + G4;
            float z1 = z0 - k1 + G4;
            float w1 = w0 - l1 + G4;
            float x2 = x0 - i2 + 2f * G4;
            float y2 = y0 - j2 + 2f * G4;
            float z2 = z0 - k2 + 2f * G4;
            float w2 = w0 - l2 + 2f * G4;
            float x3 = x0 - i3 + 3f * G4;
            float y3 = y0 - j3 + 3f * G4;
            float z3 = z0 - k3 + 3f * G4;
            float w3 = w0 - l3 + 3f * G4;
            float x4 = x0 - 1f + 4f * G4;
            float y4 = y0 - 1f + 4f * G4;
            float z4 = z0 - 1f + 4f * G4;
            float w4 = w0 - 1f + 4f * G4;

            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;
            int ll = l & 255;

            int gi0 = Perm[ii      + Perm[jj      + Perm[kk      + Perm[ll     ]]]] % 32;
            int gi1 = Perm[ii + i1 + Perm[jj + j1 + Perm[kk + k1 + Perm[ll + l1]]]] % 32;
            int gi2 = Perm[ii + i2 + Perm[jj + j2 + Perm[kk + k2 + Perm[ll + l2]]]] % 32;
            int gi3 = Perm[ii + i3 + Perm[jj + j3 + Perm[kk + k3 + Perm[ll + l3]]]] % 32;
            int gi4 = Perm[ii + 1  + Perm[jj + 1  + Perm[kk + 1  + Perm[ll + 1 ]]]] % 32;

            float n0 = ContribWeight4(x0, y0, z0, w0, gi0);
            float n1 = ContribWeight4(x1, y1, z1, w1, gi1);
            float n2 = ContribWeight4(x2, y2, z2, w2, gi2);
            float n3 = ContribWeight4(x3, y3, z3, w3, gi3);
            float n4 = ContribWeight4(x4, y4, z4, w4, gi4);

            return 27f * (n0 + n1 + n2 + n3 + n4);
        }

        private static float ContribWeight4(float dx, float dy, float dz, float dw, int gi)
        {
            float t = 0.6f - dx * dx - dy * dy - dz * dz - dw * dw;
            if (t < 0f) return 0f;
            t *= t;
            return t * t * Dot4(gi, dx, dy, dz, dw);
        }

        // ---- Vector overloads -------------------------------------------------

        /// <summary>Vector2 でサンプリング。</summary>
        public static float Evaluate(Vector2 pos) => Evaluate(pos.x, pos.y);

        /// <summary>Vector3 でサンプリング。</summary>
        public static float Evaluate(Vector3 pos) => Evaluate(pos.x, pos.y, pos.z);

        /// <summary>Vector4 でサンプリング。</summary>
        public static float Evaluate(Vector4 pos) => Evaluate(pos.x, pos.y, pos.z, pos.w);

        // ---- Utility ----------------------------------------------------------

        /// <summary>[-1,1] を [0,1] に正規化して返す。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Evaluate01(float x, float y)
            => Evaluate(x, y) * 0.5f + 0.5f;

        /// <inheritdoc cref="Evaluate01(float,float)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Evaluate01(float x, float y, float z)
            => Evaluate(x, y, z) * 0.5f + 0.5f;

        /// <inheritdoc cref="Evaluate01(float,float)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Evaluate01(Vector2 pos) => Evaluate01(pos.x, pos.y);

        /// <inheritdoc cref="Evaluate01(float,float)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Evaluate01(Vector3 pos) => Evaluate01(pos.x, pos.y, pos.z);
    }
}
