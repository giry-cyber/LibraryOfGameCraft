using System;
using UnityEngine;

namespace LibraryOfGamecraft.Terrain
{
    /// <summary>
    /// 1チャンク分の描画を担う MonoBehaviour。
    /// データの生成とメッシュの再構築を外部 (ChunkManager) から呼び出す設計。
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        // ---- プロパティ -----------------------------------------------------

        public ChunkData       Data      { get; private set; }
        public Vector2Int      ChunkPos  { get; private set; }
        public bool            IsReady   { get; private set; }

        // ---- 内部フィールド -------------------------------------------------

        private MeshFilter   _filter;
        private MeshCollider _collider;

        // ---- Unity ライフサイクル -------------------------------------------

        private void Awake()
        {
            _filter   = GetComponent<MeshFilter>();
            _collider = GetComponent<MeshCollider>();
        }

        private void OnDestroy()
        {
            DestroyCurrentMesh();
        }

        // ---- 公開 API -------------------------------------------------------

        /// <summary>
        /// チャンクを初期化する。プール再利用時にも呼ぶ。
        /// </summary>
        public void Initialize(Vector2Int chunkPos, Material material)
        {
            ChunkPos = chunkPos;
            IsReady  = false;
            Data     = new ChunkData();
            DestroyCurrentMesh();

            transform.position = new Vector3(
                chunkPos.x * ChunkData.Width,
                0f,
                chunkPos.y * ChunkData.Depth);

            gameObject.name = $"Chunk ({chunkPos.x}, {chunkPos.y})";

            var mr = GetComponent<MeshRenderer>();
            mr.sharedMaterial = material;
        }

        /// <summary>
        /// ボクセルデータをセットし、メッシュを構築する。
        /// </summary>
        /// <param name="data">生成済み ChunkData</param>
        /// <param name="worldBlockAt">チャンク境界越えのブロック参照コールバック</param>
        public void Apply(ChunkData data, Func<int, int, int, BlockType> worldBlockAt)
        {
            Data = data;
            RebuildMesh(worldBlockAt);
            IsReady = true;
        }

        /// <summary>
        /// メッシュだけ再構築する (隣接チャンクが後からロードされたときなど)。
        /// </summary>
        public void RebuildMesh(Func<int, int, int, BlockType> worldBlockAt)
        {
            DestroyCurrentMesh();

            var mesh = MeshBuilder.Build(Data, worldBlockAt);
            _filter.sharedMesh   = mesh;
            _collider.sharedMesh = mesh;
        }

        // ---- 内部ヘルパー ---------------------------------------------------

        private void DestroyCurrentMesh()
        {
            var old = _filter != null ? _filter.sharedMesh : null;
            if (old != null)
            {
                _filter.sharedMesh   = null;
                _collider.sharedMesh = null;
                Destroy(old);
            }
        }
    }
}
