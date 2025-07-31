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

        // Old DO NOT USE
        public PbrTextures PbrTextures = new PbrTextures();
        public int materialTypeIndex = 0;
        public bool transparent = false;
        // public int pbrWorkflowIndex = 0;
        public bool shouldCreateNewMaterial = true;
    }
    public class PbrTextures : AssetStructure
    {
        public Texture DiffuseMap = new Texture();
        public Texture MetalnessMap = new Texture();
        public Texture RoughnessMap = new Texture();
        public Texture SpecularMap = new Texture();
        public Texture NormalMap = new Texture();
        public Texture HeightMap = new Texture();
        public Texture EmissiveMap = new Texture();
        public Texture AlphaMap = new Texture();
        public List<Texture> UnsortedTextures = new List<Texture>(); // To make life a little easier I guess.
    }
}
