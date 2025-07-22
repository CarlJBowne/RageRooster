using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectRestructuring
{
    public class AssetStructure
    {
        public Prefab finalAssetPrefab;
        public Model fbxAsset;
        public Material material;
        public PbrTextures textures;
        public List<AssetBase> unsortedAssets;

        public void RenameAssets()
        {
            finalAssetPrefab.RenameAsset();
            foreach (AssetBase i in unsortedAssets)
            {
                i.RenameAsset();
            }
            
            fbxAsset?.RenameAsset();
            material?.RenameAsset();
            textures?.RenameAssets();
        }
    }
    public abstract class AssetBase
    {
        public abstract string GetPrefix();
        public string path;

        public AssetBase(string _path) // Abstract constructor
        {
            path = _path;
        }
        public virtual void RenameAsset()
        {
            if (PRUtilities.GetFilename(path).StartsWith(GetPrefix())) return;

            string oldName = PRUtilities.GetFilename(path);
            string newName = oldName;
            newName = GetPrefix() + newName;
            string renamedResult = AssetDatabase.RenameAsset(path, newName);
            path = PRUtilities.GetPathWithoutFilename(path) + "\\" + newName;
            Debug.Log("Renamed to " + renamedResult + " with path " + path);
        }
    }
    public class Prefab : AssetBase
    {
        public override string GetPrefix() => "pf_";
        public static List<string> extensions = new List<string>() { ".prefab" };
        public Prefab(string _path) : base(_path) { } // Constructor
    }
    public class Model : AssetBase
    {
        public override string GetPrefix() => "geo_";
        public static List<string> extensions = new List<string>() { ".fbx" };
        public Model(string _path) : base(_path) { } // Constructor
    }
    public class Texture : AssetBase
    {
        public override string GetPrefix() => "tex_";
        public static List<string> extensions = new List<string>() { ".png" };
        public Texture(string _path) : base(_path) { }  // Constructor
    }
    public class Material : AssetBase
    {
        public override string GetPrefix() => "mat_";
        public static List<string> extensions = new List<string>() { ".asset" };
        public Material(string _path) : base(_path) {}
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
            // Using reflection to automate looping through the fields would be overkill so just brute forcing it :)
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

        public static string GetPathWithoutFilename(string path)
        {
            return System.IO.Path.GetDirectoryName(path);
        }
    }
}
