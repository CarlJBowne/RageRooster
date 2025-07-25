using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetImportPipeline
{
    public abstract class AssetBase
    {
        public abstract string GetPrefix();
        public string assetPath = "No filepath set!";
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
        public static List<string> extensions = new List<string>() { ".asset" };
    }
}
