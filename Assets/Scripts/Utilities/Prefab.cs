using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.SceneManagement;

/// <summary>
/// A representation of a Prefab.
/// </summary>
[System.Serializable]
public class Prefab
{
    [field: SerializeField]
    public string path { get; private set; }
    public bool opened { get; private set; } = false;
    public GameObject readOnlyObject { get
        {
            if (_readOnlyObjectCache == null) _readOnlyObjectCache = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return _readOnlyObjectCache;
        }  
    }
    private GameObject _readOnlyObjectCache = null;
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
        if (!withoutSaving)
        {
            EditorSceneManager.MarkSceneDirty(editableObject.scene); // Mark the scene as dirty  
            PrefabUtility.SaveAsPrefabAsset(editableObject, path);
        }
        PrefabUtility.UnloadPrefabContents(editableObject);
        opened = false;
    }
}