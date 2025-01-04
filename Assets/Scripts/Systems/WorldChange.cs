using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WorldChange : ScriptableObject
{
    private bool _enabled;

    private System.Action _activateAction;

    public bool Enabled => _enabled;
    public System.Action Action => _activateAction;


    public void Activate()
    {
        _activateAction?.Invoke();
        _enabled = true;
    }
    public void Deactivate() => _enabled = false;

    [EditorAttributes.Button]
    public void Delete()
    {

        if (EditorUtility.DisplayDialog("Remove World Change", "Are you sure you want to delete this world change?", "Yes", "No"))
        {
            Undo.RecordObject(this, "World Change Deleted");
            DestroyImmediate(this, true);
            AssetDatabase.SaveAssets();
        }
    }

}
