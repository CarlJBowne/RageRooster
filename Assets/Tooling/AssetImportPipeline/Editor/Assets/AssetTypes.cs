using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetImportPipeline
{
    public abstract class AssetBase
    {
        public abstract string GetPrefix();
        public string sourcePath = "No filepath set!";
        public string destinationPath = "";
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
    }
    public class Prefab : AssetBase
    {
        public override string GetPrefix() => "pf_";
        public static List<string> extensions = new List<string>() { ".prefab" };
    }
    public class Model : AssetBase
    {
        public override string GetPrefix() => "geo_";
        public static List<string> extensions = new List<string>() { ".fbx" };
    }
    public class Texture : AssetBase
    {
        public override string GetPrefix() => "tex_";
        public static List<string> extensions = new List<string>() { ".png" };
    }
    public class Material : AssetBase
    {
        public override string GetPrefix() => "mat_";
        public static List<string> extensions = new List<string>() { ".asset" }; // should actually be .mat i was dumb, not changing it yet just in case to remind myself in case something is Wrong
    }
}
