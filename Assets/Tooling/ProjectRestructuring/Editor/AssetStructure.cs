using Codice.CM.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectRestructuring
{
    public class AssetStructure
    {
        public Prefab finalAssetPrefab;
        public Model fbxAsset;
        public Material material;
        public PbrTextures textures;
        public List<AssetBase> unsortedAssets;
        public List<AssetBase> uncertainAssets;

        public void RenameAssets()
        {
            finalAssetPrefab.RenameAsset();
            //fbxAsset.RenameAsset();
            //material.RenameAsset();
            //textures.RenameAssets();
        }

        public void DebugAssetStructure()
        {
            string message = "AssetStructure: " + finalAssetPrefab.path;// + fbxAsset.path + material.path + textures.diffuse.path + textures.roughness.path + textures.specular.path + textures.normal.path + textures.height.path;
            Debug.Log(message);
        }
    }
    public abstract class AssetBase
    {
        public string path;

        public AssetBase(string _path) // Abstract constructor
        {
            path = _path;
        }
        public abstract void RenameAsset();
    }
    public class Prefab : AssetBase
    {
        public string prefix = "pf_";
        public static List<string> extensions = new List<string>() { ".prefab" };
        public Prefab(string _path) : base(_path) { } // Constructor

        public override void RenameAsset()
        {
            // CharacterName
            string originalName = PRUtilities.GetFilename(path);
            string newName;
        }
    }
    public class Model : AssetBase
    {
        public string prefix = "geo_";
        public static List<string> extensions = new List<string>() { ".fbx" };
        public Model(string _path) : base(_path) { } // Constructor

        public override void RenameAsset()
        {

        }
    }
    public class Texture : AssetBase
    {
        public string prefix = "tex_";
        public static List<string> extensions = new List<string>() { ".png" };
        public Texture(string _path) : base(_path) { }  // Constructor

        public override void RenameAsset()
        {

        }
    }
    public class Material : AssetBase
    {
        public string prefix = "mat_";
        public static List<string> extensions = new List<string>() { ".asset" };
        public Material(string _path) : base(_path) {}
        public override void RenameAsset()
        {
            throw new System.NotImplementedException();
        }
    }
    public class PbrTextures
    {
        public Texture diffuse;
        public Texture roughness;
        public Texture specular;
        public Texture normal;
        public Texture height;
        public List<Texture> unsortedTextures; // To make life a little easier I guess.

        public void RenameAssets()
        {
            // Using reflection to automate looping through the fields seems like it would be overkill so just brute forcing it :)
            diffuse.RenameAsset();
            roughness.RenameAsset();
            specular.RenameAsset();
            normal.RenameAsset();
            height.RenameAsset();
        }
    }
    public static class PRUtilities
    {
        public static string GetFilename(string path)
        {
            return System.IO.Path.GetFileName(path);
        }

        public static string GetFilenameWithoutExtension(string path)
        {
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public static string GetFileExtension(string path)
        {
            return System.IO.Path.GetExtension(path);
        }
    }
}
