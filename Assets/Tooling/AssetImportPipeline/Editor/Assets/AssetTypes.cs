using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetImportPipeline
{
    public abstract class AssetBase
    {
        public abstract string GetPrefix();
        public string path;

        public AssetBase(string _path) // Abstract constructor
        {
            path = _path;
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
}
