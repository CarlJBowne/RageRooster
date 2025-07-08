using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DependencyFinder
{
    public string[] FindDependencies(string path)
    {
        string[] dependencies = AssetDatabase.GetDependencies(path);
        return dependencies;
    }

    public void MoveDependencies(string path)
    {
        List<string> allAssetPaths = new List<string>();

        allAssetPaths.Add(path);

        allAssetPaths.AddRange(FindDependencies(path));

        foreach (string assetPath in allAssetPaths)
        {
            AssetDatabase.MoveAsset(assetPath, "Assets/" + GetFilename(assetPath));
            Debug.Log("Moved " + GetFilename(assetPath));
        }
        
        //AssetDatabase.MoveAsset(path);
    }

    string GetFilename(string path)
    {
        return System.IO.Path.GetFileName(path);
    }
}
