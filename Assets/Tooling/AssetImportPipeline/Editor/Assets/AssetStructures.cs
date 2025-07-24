using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetImportPipeline
{
    public abstract class AssetStructure { }
    public class StaticMesh : AssetStructure
    {
        public Prefab prefab;
        public Model model;
        public Material material;
        public PbrTextures textures;
    }
    public class PbrTextures : AssetStructure
    {
        public Texture DiffuseMap;
        public Texture RoughnessMap;
        public Texture SpecularMap;
        public Texture NormalMap;
        public Texture HeightMap;
        public List<Texture> UnsortedTextures; // To make life a little easier I guess.
    }
}
