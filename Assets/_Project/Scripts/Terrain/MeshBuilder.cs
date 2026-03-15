using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// ChunkData からメッシュを構築する静的クラス。
    /// 隣接ブロックが不透明な面はスキップし DrawCall を最小化する (可視面カリング)。
    /// 頂点カラーを出力するため、マテリアルは VertexColor シェーダを推奨。
    /// </summary>
    public static class MeshBuilder
    {
        // ---- 面ごとの頂点オフセット (ブロック原点0,0,0 基準) ----------------

        // 頂点順: 左下 → 左上 → 右上 → 右下 (反時計回り = 外向き法線)
        private static readonly Vector3[][] s_FaceVerts =
        {
            // 0: Top (+Y)
            new[] { new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0) },
            // 1: Bottom (-Y)
            new[] { new Vector3(0,0,1), new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1) },
            // 2: North (+Z)
            new[] { new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1), new Vector3(0,0,1) },
            // 3: South (-Z)
            new[] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0) },
            // 4: East (+X)
            new[] { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(1,0,1) },
            // 5: West (-X)
            new[] { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0), new Vector3(0,0,0) },
        };

        private static readonly Vector3[] s_FaceNormals =
        {
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back,
            Vector3.right, Vector3.left,
        };

        private static readonly Vector3Int[] s_FaceDirs =
        {
            Vector3Int.up, Vector3Int.down,
            new(0, 0, 1), new(0, 0, -1),
            Vector3Int.right, Vector3Int.left,
        };

        // 面の明るさ係数 (環境遮蔽風の簡易シェーディング)
        private static readonly float[] s_FaceBrightness = { 1.0f, 0.5f, 0.8f, 0.8f, 0.9f, 0.9f };

        // 1 quad = 2 三角形, 頂点インデックスオフセット
        private static readonly int[] s_QuadIndices = { 0, 1, 2, 0, 2, 3 };

        // ---- 公開 API -------------------------------------------------------

        /// <summary>
        /// チャンクデータからメッシュを生成する。
        /// </summary>
        /// <param name="data">対象チャンクのボクセルデータ</param>
        /// <param name="worldBlockAt">
        ///     チャンク境界をまたいで隣接ブロックを取得するコールバック。
        ///     引数はローカル座標 (チャンク外の値も渡る)。
        /// </param>
        public static Mesh Build(ChunkData data, Func<int, int, int, BlockType> worldBlockAt)
        {
            var verts   = new List<Vector3>();
            var tris    = new List<int>();
            var uvs     = new List<Vector2>();
            var normals = new List<Vector3>();
            var colors  = new List<Color>();

            var registry  = BlockRegistry.Instance;
            int atlasSize = registry?.atlasSize ?? 1;

            for (int lz = 0; lz < ChunkData.Depth;  lz++)
            for (int ly = 0; ly < ChunkData.Height; ly++)
            for (int lx = 0; lx < ChunkData.Width;  lx++)
            {
                var block = data.GetBlock(lx, ly, lz);
                if (block == BlockType.Air) continue;

                var def = registry != null
                    ? registry.Get(block)
                    : BlockRegistry.GetDefault(block);

                var blockOffset = new Vector3(lx, ly, lz);

                for (int f = 0; f < 6; f++)
                {
                    var d  = s_FaceDirs[f];
                    var nx = lx + d.x;
                    var ny = ly + d.y;
                    var nz = lz + d.z;

                    // 隣接ブロックが solid なら面を追加しない
                    BlockType neighbor = ChunkData.IsOutOfBounds(nx, ny, nz)
                        ? worldBlockAt(nx, ny, nz)
                        : data.GetBlock(nx, ny, nz);

                    if (IsSolid(neighbor, registry)) continue;

                    // ---- 頂点 -----------------------------------------------
                    int baseIdx = verts.Count;
                    foreach (var v in s_FaceVerts[f])
                        verts.Add(blockOffset + v);

                    // ---- インデックス ----------------------------------------
                    foreach (var i in s_QuadIndices)
                        tris.Add(baseIdx + i);

                    // ---- 法線 -----------------------------------------------
                    var n = s_FaceNormals[f];
                    for (int k = 0; k < 4; k++) normals.Add(n);

                    // ---- 頂点カラー -----------------------------------------
                    float bright = s_FaceBrightness[f];
                    var   c      = def.color * bright;
                    c.a = 1f;
                    for (int k = 0; k < 4; k++) colors.Add(c);

                    // ---- UV -------------------------------------------------
                    Vector2Int tile = f == 0 ? def.topTile
                                    : f == 1 ? def.bottomTile
                                    : def.sideTile;
                    AppendQuadUV(uvs, tile, atlasSize);
                }
            }

            var mesh = new Mesh { name = "ChunkMesh" };

            // 65535 頂点超えに対応
            if (verts.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---- 内部ヘルパー ---------------------------------------------------

        private static bool IsSolid(BlockType type, BlockRegistry registry)
        {
            if (type == BlockType.Air) return false;
            return registry != null
                ? registry.Get(type).isSolid
                : BlockRegistry.GetDefault(type).isSolid;
        }

        private static void AppendQuadUV(List<Vector2> uvs, Vector2Int tile, int atlasSize)
        {
            float ts = 1f / Mathf.Max(atlasSize, 1);
            float u  = tile.x * ts;
            float v  = tile.y * ts;
            uvs.Add(new Vector2(u,      v     ));
            uvs.Add(new Vector2(u,      v + ts));
            uvs.Add(new Vector2(u + ts, v + ts));
            uvs.Add(new Vector2(u + ts, v     ));
        }
    }
}
