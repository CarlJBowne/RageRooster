using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Policy;
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
        public bool useExistingMaterial = false;
        public enum Shaders { CelShaderLit, UniversalRenderPipelineLit }
        public Shaders shader = Shaders.CelShaderLit;
        public string fbxMaterialSlotName = ""; // Storing this value redundantly on purpose because it needs to be immutable after being set.

        public string StripMaterialName()
        {
            foreach (string i in new[] { "m_", "mat_" })
                if (customName.StartsWith(i, StringComparison.OrdinalIgnoreCase)) customName = customName.Substring(i.Length);
            return customName;
        }
        public string GetMaterialName(StaticMesh staticMesh)
        {
            return staticMesh.assetName + "-" + customName;
        }

        public URPLitSettings urplSettings = new URPLitSettings();
        public CelShaderLitSettings cslSettings = new CelShaderLitSettings();

        string prefix = "mat_";
        public override string GetPrefix() => prefix;
        public override string SetPrefix(string _prefix) => prefix = _prefix;

        protected override AssetBase GetNewAssetOfSubclass()
        {
            return new Material();
        }
        public static List<string> extensions = new List<string>() { ".asset" }; // should actually be .mat i was dumb, not changing it yet just in case to remind myself in case something is Wrong

        public abstract class ShaderSettings{}
        public class CelShaderLitSettings : ShaderSettings
        {
            public Texture BaseColor = new Texture();
            public Texture NormalMap = new Texture();
            public Texture HeightMap = new Texture();
            public Texture AO = new Texture();
        }
        public class URPLitSettings : ShaderSettings // Please note this does not currently correspond to the URPL shader
        {
            public bool transparent = false;
            public Texture DiffuseMap = new Texture();
            public Texture MetalnessMap = new Texture();
            public Texture RoughnessMap = new Texture();
            public Texture SpecularMap = new Texture();
            public Texture NormalMap = new Texture();
            public Texture HeightMap = new Texture();
            public Texture EmissiveMap = new Texture();
            public Texture AlphaMap = new Texture();
        }
    }
}
