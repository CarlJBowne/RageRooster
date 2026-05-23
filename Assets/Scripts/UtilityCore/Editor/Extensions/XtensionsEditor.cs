using System.Collections.Generic;
using System.IO;
using System.Linq;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class XtensionsEditor
{

}

public struct Asset<T> where T : UnityEngine.Object
{
    public GUID GUID;
    public string path;
    public T Value;

    public Asset(GUID input, bool openImmediately = false)
    {
        GUID = input;
        path = AssetDatabase.GUIDToAssetPath(GUID);

        Value = openImmediately ? AssetDatabase.LoadAssetByGUID<T>(GUID) : null;
    }
    public Asset(string input, bool openImmediately = false)
    {
        path = input;
        GUID = AssetDatabase.GUIDFromAssetPath(path);

        Value = openImmediately ? AssetDatabase.LoadAssetByGUID<T>(GUID) : null;
    }

    public void Load() => Value = AssetDatabase.LoadAssetByGUID<T>(GUID);
}

public class AssemblyDefinitionAsset
{
    public static Dictionary<string, AssemblyDefinitionAsset> Loaded = new();

    public static AssemblyDefinitionAsset Load(string inputGUID)
    {
        if (Loaded.ContainsKey(inputGUID)) return Loaded[inputGUID];
        if (!AssetDatabase.GUIDToAssetPath(inputGUID).EndsWith(".asmdef")) return null;

        AssemblyDefinitionAsset T = new();
        T.LoadThis(inputGUID);
        Loaded[inputGUID] = T;
        return T;
    }

    public void LoadThis(string inputGUID)
    {
        GUID = inputGUID;
        Path = (Application.dataPath + AssetDatabase.GUIDToAssetPath(inputGUID))
                .Replace("AssetsAssets", "Assets").Replace('\\', '/');
        Name = System.IO.Path.GetFileNameWithoutExtension(Path);
        rootJObject = JObject.Parse(File.OpenText(Path).ReadToEnd());
        JArray jReferences = rootJObject["references"] as JArray;

        References = jReferences == null ? new()
            : jReferences.ToObject<IEnumerable<string>>()
            .Select(IN => IN.Replace("GUID:", ""))
            .ToHashSet();
    }
    public void Save()
    {
        List<string> export = References.Select(IN => IN.StartsWith("GUID:") ? IN : IN.Insert(0, "GUID:")).ToList();

        rootJObject["references"] = JArray.FromObject(export);

        try
        {
            using StreamWriter file = File.CreateText(Path);
            file.WriteLine(rootJObject);
        }
        catch
        {
            Debug.LogWarning($"Unable to save automatic changes to Assembly Definition: {Name}");
        }
    }

    public string GUID;
    public string Path;
    public string Name;

    JObject rootJObject;
    public HashSet<string> References;


}