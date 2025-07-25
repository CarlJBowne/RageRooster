using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetImportPipeline
{
    public abstract class AssetStructure { }
    public class StaticMesh : AssetStructure
    {
        public Prefab prefab = new Prefab();
        public Model model = new Model();
        public Material material = new Material();
        public PbrTextures PbrTextures = new PbrTextures();
    }
    public class PbrTextures : AssetStructure
    {
        public Texture DiffuseMap = new Texture();
        public Texture RoughnessMap = new Texture();
        public Texture SpecularMap = new Texture();
        public Texture NormalMap = new Texture();
        public Texture HeightMap = new Texture();
        public List<Texture> UnsortedTextures = new List<Texture>(); // To make life a little easier I guess.
    }
}
