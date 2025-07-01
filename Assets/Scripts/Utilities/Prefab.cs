using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// A representation of a Prefab.
/// </summary>
[System.Serializable]
public class Prefab
{
    public string path { get; private set; }
    public bool opened { get; private set; } = false;
    public GameObject readOnlyObject => AssetDatabase.LoadAssetAtPath<GameObject>(path);
    public GameObject editableObject { get; private set; } = null;

    public Prefab(string path, bool openForEditing = false)
    {
        this.path = path;
        if (readOnlyObject == null)
        {
            Debug.LogError($"Failed to find a prefab at path: {path}");
            return;
        }
        if (openForEditing) Open();
    }

    public void Open()
    {
        if (opened) return;
        editableObject = PrefabUtility.LoadPrefabContents(path);
        if (editableObject == null)
        {
            Debug.LogError($"Failed to open prefab at path: {path}");
            return;
        }
        opened = true;
    }

    public void Close(bool withoutSaving = false)
    {
        if (!opened) return;
        if(!withoutSaving) PrefabUtility.SaveAsPrefabAsset(editableObject, path);
        PrefabUtility.UnloadPrefabContents(editableObject);
        opened = false;
    }
}