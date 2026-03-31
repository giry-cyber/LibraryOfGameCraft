using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfGamecraft.MineCraftLikeTerrain
{
    /// <summary>
    /// プレイヤー周辺のチャンクを動的にロード/アンロードする。
    /// コルーチンで1フレームに1チャンクずつ生成し、フリーズを防ぐ。
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        // ---- インスペクタ設定 -----------------------------------------------

        [Header("参照")]
        [SerializeField] private Transform      player;
        [SerializeField] private Material       chunkMaterial;
        [SerializeField] private TerrainSettings terrainSettings;

        [Header("ロード設定")]
        [Tooltip("プレイヤーを中心にロードするチャンク半径")]
        [SerializeField, Range(2, 16)] private int viewDistance = 6;

        [Tooltip("1フレームに生成するチャンク数 (重くなる場合は1に下げる)")]
        [SerializeField, Range(1, 4)] private int chunksPerFrame = 1;

        [Header("オブジェクトプール")]
        [Tooltip("プールに保持する最大チャンク数")]
        [SerializeField, Range(4, 64)] private int poolSize = 32;

        // ---- 内部状態 -------------------------------------------------------

        private readonly Dictionary<Vector2Int, Chunk> _active   = new();
        private readonly Queue<Chunk>                  _pool     = new();
        private readonly Queue<Vector2Int>             _genQueue = new();
        private readonly HashSet<Vector2Int>           _queued   = new();

        private Vector2Int _lastPlayerChunk = new(int.MaxValue, int.MaxValue);

        // ---- Unity ライフサイクル -------------------------------------------

        private void Start()
        {
            if (player == null)
                player = Camera.main?.transform;

            StartCoroutine(GenerationLoop());
        }

        private void Update()
        {
            var currentChunk = WorldToChunkPos(player.position);
            if (currentChunk == _lastPlayerChunk) return;

            _lastPlayerChunk = currentChunk;
            EnqueueVisibleChunks(currentChunk);
            UnloadDistantChunks(currentChunk);
        }

        // ---- チャンク管理 ---------------------------------------------------

        private void EnqueueVisibleChunks(Vector2Int center)
        {
            // 距離順にソートして近くから生成
            var candidates = new List<(Vector2Int pos, float dist)>();

            for (int z = -viewDistance; z <= viewDistance; z++)
            for (int x = -viewDistance; x <= viewDistance; x++)
            {
                var cp = center + new Vector2Int(x, z);
                if (_active.ContainsKey(cp) || _queued.Contains(cp)) continue;

                float dist = Vector2.Distance(cp, center);
                if (dist <= viewDistance)
                    candidates.Add((cp, dist));
            }

            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            foreach (var (cp, _) in candidates)
            {
                _genQueue.Enqueue(cp);
                _queued.Add(cp);
            }
        }

        private void UnloadDistantChunks(Vector2Int center)
        {
            var toRemove = new List<Vector2Int>();

            foreach (var cp in _active.Keys)
            {
                if (Vector2.Distance(cp, center) > viewDistance + 1f)
                    toRemove.Add(cp);
            }

            foreach (var cp in toRemove)
                ReturnToPool(cp);
        }

        // ---- コルーチン生成ループ -------------------------------------------

        private IEnumerator GenerationLoop()
        {
            while (true)
            {
                int generated = 0;

                while (_genQueue.Count > 0 && generated < chunksPerFrame)
                {
                    var cp = _genQueue.Dequeue();
                    _queued.Remove(cp);

                    // 既にロード済みならスキップ
                    if (!_active.ContainsKey(cp))
                    {
                        GenerateChunk(cp);
                        generated++;
                    }
                }

                yield return null;
            }
        }

        private void GenerateChunk(Vector2Int cp)
        {
            var chunk = GetOrCreateChunk();
            chunk.Initialize(cp, chunkMaterial);
            _active[cp] = chunk;

            // ボクセルデータ生成
            var data = TerrainGenerator.Generate(cp, terrainSettings);

            // メッシュ構築 (このチャンクのワールド座標オフセットを持たせてクロージャ化)
            var capturedCp = cp;
            chunk.Apply(data, (lx, ly, lz) => GetWorldBlock(capturedCp, lx, ly, lz));

            // 隣接チャンクを再メッシュ (境界面を正しく表示するため)
            RemeshNeighbors(cp);
        }

        private void RemeshNeighbors(Vector2Int cp)
        {
            Vector2Int[] neighbors =
            {
                cp + Vector2Int.right,
                cp + Vector2Int.left,
                cp + Vector2Int.up,
                cp + Vector2Int.down,
            };

            foreach (var n in neighbors)
            {
                if (!_active.TryGetValue(n, out var neighbor) || !neighbor.IsReady)
                    continue;

                var capturedN = n;
                neighbor.RebuildMesh((lx, ly, lz) => GetWorldBlock(capturedN, lx, ly, lz));
            }
        }

        // ---- ワールドブロック参照 -------------------------------------------

        /// <summary>
        /// チャンク境界を越えたブロック参照。MeshBuilder のコールバックとして使用。
        /// </summary>
        public BlockType GetWorldBlock(Vector2Int chunkPos, int lx, int ly, int lz)
        {
            // チャンク内ならそのまま返す
            if (!ChunkData.IsOutOfBounds(lx, ly, lz))
            {
                return _active.TryGetValue(chunkPos, out var self)
                    ? self.Data.GetBlock(lx, ly, lz)
                    : BlockType.Air;
            }

            // ワールド座標に変換して隣接チャンクを参照
            int wx = chunkPos.x * ChunkData.Width  + lx;
            int wy = ly;
            int wz = chunkPos.y * ChunkData.Depth  + lz;

            if (wy < 0 || wy >= ChunkData.Height)
                return BlockType.Air;

            int targetCx = Mathf.FloorToInt((float)wx / ChunkData.Width);
            int targetCz = Mathf.FloorToInt((float)wz / ChunkData.Depth);
            int localX   = wx - targetCx * ChunkData.Width;
            int localZ   = wz - targetCz * ChunkData.Depth;

            var targetCp = new Vector2Int(targetCx, targetCz);
            return _active.TryGetValue(targetCp, out var target)
                ? target.Data.GetBlock(localX, wy, localZ)
                : BlockType.Air;
        }

        // ---- オブジェクトプール ---------------------------------------------

        private Chunk GetOrCreateChunk()
        {
            if (_pool.Count > 0)
            {
                var pooled = _pool.Dequeue();
                pooled.gameObject.SetActive(true);
                return pooled;
            }

            var go = new GameObject("Chunk");
            go.transform.SetParent(transform);
            return go.AddComponent<Chunk>();
        }

        private void ReturnToPool(Vector2Int cp)
        {
            if (!_active.TryGetValue(cp, out var chunk)) return;
            _active.Remove(cp);

            if (_pool.Count < poolSize)
            {
                chunk.gameObject.SetActive(false);
                _pool.Enqueue(chunk);
            }
            else
            {
                Destroy(chunk.gameObject);
            }
        }

        // ---- ユーティリティ -------------------------------------------------

        private static Vector2Int WorldToChunkPos(Vector3 worldPos)
            => new(
                Mathf.FloorToInt(worldPos.x / ChunkData.Width),
                Mathf.FloorToInt(worldPos.z / ChunkData.Depth));

        // ---- エディタ可視化 -------------------------------------------------

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (player == null) return;
            var center = WorldToChunkPos(player.position);
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);

            for (int z = -viewDistance; z <= viewDistance; z++)
            for (int x = -viewDistance; x <= viewDistance; x++)
            {
                var cp = center + new Vector2Int(x, z);
                if (Vector2.Distance(cp, center) > viewDistance) continue;

                var wPos = new Vector3(
                    cp.x * ChunkData.Width  + ChunkData.Width  * 0.5f,
                    ChunkData.Height * 0.5f,
                    cp.y * ChunkData.Depth  + ChunkData.Depth  * 0.5f);

                Gizmos.DrawWireCube(wPos, new Vector3(
                    ChunkData.Width, ChunkData.Height, ChunkData.Depth));
            }
        }
#endif
    }
}
