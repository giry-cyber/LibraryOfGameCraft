using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LibraryOfGamecraft.WorldStreaming
{
    // WorldStreaming でシーンがロードされるたびに、そのシーン内の NavMeshSurface をBakeする。
    // WorldStreamingHub と同じ GameObject にアタッチする。
    [RequireComponent(typeof(WorldStreamingHub))]
    public class WorldNavMeshManager : MonoBehaviour
    {
        private WorldStreamingHub _hub;

        private void Awake() => _hub = GetComponent<WorldStreamingHub>();

        private void Start()
        {
            // 既にロード済みのシーンを処理（テスト用などで事前にロードされている場合）
            foreach (var grid in _hub.GetLoadedGrids())
            {
                if (_hub.TryGetLoadedScene(grid, out var scene))
                    BakeScene(grid, scene);
            }
        }

        private void OnEnable()  => _hub.OnSceneLoaded += BakeScene;
        private void OnDisable() => _hub.OnSceneLoaded -= BakeScene;

        private void BakeScene(Vector2Int grid, Scene scene)
        {
            var surface = FindSurfaceInScene(scene);
            if (surface == null)
            {
                Debug.LogWarning($"[WorldNavMeshManager] {scene.name} に NavMeshSurface がありません。シーンに NavMeshSurface コンポーネントを追加してください。");
                return;
            }
            surface.BuildNavMesh();
            Debug.Log($"[WorldNavMeshManager] NavMesh Bake 完了: {scene.name} (Grid {grid})");
        }

        private static NavMeshSurface FindSurfaceInScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var surface = root.GetComponentInChildren<NavMeshSurface>(true);
                if (surface != null) return surface;
            }
            return null;
        }
    }
}
