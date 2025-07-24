using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetImportPipeline
{
    public class StaticMesh
    {
        public Prefab finalAssetPrefab;
        public Model fbxAsset;
        public Material material;
        public PbrTextures textures;
    }
    public class PbrTextures
    {
        public Texture diffuse;
        public Texture roughness;
        public Texture specular;
        public Texture normal;
        public Texture height;
        public List<Texture> unsortedTextures; // To make life a little easier I guess.
    }
}
