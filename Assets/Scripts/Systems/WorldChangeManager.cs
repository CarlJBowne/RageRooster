using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldChangeManager", menuName = "ScriptableObjects/WorldChangeManager")]
public class WorldChangeManager : SingletonScriptable<WorldChangeManager>
{
    [EditorAttributes.Button]
    public void NewWorldChange()
    {
        WorldChange newChange = CreateInstance<WorldChange>();
        newChange.name = "New Change";
        AssetDatabase.AddObjectToAsset(newChange, this);
        AssetDatabase.SaveAssetIfDirty(newChange);
        Undo.RegisterCreatedObjectUndo(newChange, "Added World Change");
    }
}
