using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DependencyFinder
{
    public void FindDependencies()
    {
        string[] dependencies = AssetDatabase.GetDependencies("Assets/Tooling/ProjectRestructuring/TestData/OriginalLocation/exportCopy/Prefabs/cubeguyPrefab.prefab");
        foreach (string dependency in dependencies)
        {
            Debug.Log("Dependency Found! " + dependency);
        }
    }
}
