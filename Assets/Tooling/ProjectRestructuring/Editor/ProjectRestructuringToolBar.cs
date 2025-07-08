using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ProjectRestructuringToolBar : EditorWindow
{
    [MenuItem("Rage Rooster Tooling/Project Restructuring/Open Tool UI")]
    public static void ShowWindow()
    {
        GetWindow<ProjectRestructuringToolBar>("Project Restructuring");
    }

    DependencyFinder finder = new DependencyFinder();

    private void OnGUI()
    {
        if (GUILayout.Button("Test Button"))
        {
            Debug.Log("The Test Button has been pressed!");
            finder.FindDependencies("Assets/Tooling/ProjectRestructuring/TestData/OriginalLocation/exportCopy/Prefabs/cubeguyPrefab.prefab");
        }
        if (GUILayout.Button("Move Asset Test"))
        {
            finder.MoveDependencies("Assets/Tooling/ProjectRestructuring/TestData/OriginalLocation/exportCopy/Prefabs/cubeguyPrefab.prefab");

        }
    }

}
