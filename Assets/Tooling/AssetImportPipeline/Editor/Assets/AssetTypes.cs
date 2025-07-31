using System;
using System.Collections.Generic;
using System.Xml.Linq;
using EditorAttributes;
using UnityEditor;
using UnityEngine;

namespace AssetImportPipeline
{
    public abstract class AssetBase
    {
        public abstract string GetPrefix();
        public abstract string SetPrefix(string _prefix);
        public string sourcePath = "No filepath set!";
        public string destinationPath = "";
        public string customName = ""; // Allows additional sub-name for sub-assets in addition to the prefab's primary name.
        public bool SourceExists()
        {
            throw new NotImplementedException();
        }
        public bool AssetExists()
        {
            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(destinationPath));
        }
        public virtual string CreateFilename(string assetName)
        {
            string filename = GetPrefix() + assetName + Utilities.GetFileExtension(sourcePath);
            return filename;
        }
        public AssetBase ResetAsset(bool resetCustomPrefix = true)
        {
            AssetBase newAsset = GetNewAssetOfSubclass();
            if (!resetCustomPrefix) newAsset.SetPrefix(this.GetPrefix());
            return newAsset;
        }
        protected abstract AssetBase GetNewAssetOfSubclass();
    }
    public class Prefab : AssetBase
    {
        string prefix = "pf_";
        public override string GetPrefix() => prefix;
        public override string SetPrefix(string _prefix) => prefix = _prefix;

        protected override AssetBase GetNewAssetOfSubclass()
        {
            return new Prefab();
        }

        public static List<string> extensions = new List<string>() { ".prefab" };
        
    }
    public class Model : AssetBase
    {
        string prefix = "geo_";
        public bool hasBeenAnalysed = false;
        public override string GetPrefix() => prefix;
        public override string SetPrefix(string _prefix) => prefix = _prefix;
        protected override AssetBase GetNewAssetOfSubclass()
        {
            return new Model();
        }
        public static List<string> extensions = new List<string>() { ".fbx" };
    }
    public class Texture : AssetBase
    {
        string prefix = "tex_";
        public override string GetPrefix() => prefix;
        public override string SetPrefix(string _prefix) => prefix = _prefix;
        protected override AssetBase GetNewAssetOfSubclass()
        {
            return new Texture();
        }
        public static List<string> extensions = new List<string>() { ".png" };
    }
    public class Material : AssetBase
    {
        string prefix = "mat_";
        public override string GetPrefix() => prefix;
        public override string SetPrefix(string _prefix) => prefix = _prefix;



        protected override AssetBase GetNewAssetOfSubclass()
        {
            return new Material();
        }
        public static List<string> extensions = new List<string>() { ".asset" }; // should actually be .mat i was dumb, not changing it yet just in case to remind myself in case something is Wrong
    }
}
