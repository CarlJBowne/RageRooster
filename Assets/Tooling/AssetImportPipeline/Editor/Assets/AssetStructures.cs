using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetImportPipeline
{
    public abstract class AssetStructure { }
    public class StaticMesh : AssetStructure
    {
        public string assetName = "UnnamedAsset";
        public string assetCategory = "Assets/Art";
        public Prefab prefab = new Prefab();
        public Model model = new Model();
        public List<Material> materials = new List<Material>();
    }
}
