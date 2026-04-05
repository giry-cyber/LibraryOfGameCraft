using UnityEngine;

namespace LibraryOfGamecraft.Building
{
    /// <summary>
    /// プロシージャル建物生成の起点となる MonoBehaviour。
    /// ProBuilder Shape オブジェクトにアタッチして使用する。
    /// </summary>
    [RequireComponent(typeof(SemanticStore))]
    [RequireComponent(typeof(GeneratedObjectRegistry))]
    public class BuildingAuthoring : MonoBehaviour
    {
        [SerializeField] public GenerationSettings generationSettings = new();
        [SerializeField] public BuildingModuleCatalog catalog;
        [SerializeField] public bool showSemanticOverlay = true;

        private SemanticStore _semanticStore;
        private GeneratedObjectRegistry _registry;

        public SemanticStore SemanticStore
            => _semanticStore != null ? _semanticStore : (_semanticStore = GetComponent<SemanticStore>());

        public GeneratedObjectRegistry Registry
            => _registry != null ? _registry : (_registry = GetComponent<GeneratedObjectRegistry>());
    }
}
