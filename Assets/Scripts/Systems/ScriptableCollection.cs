using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSOCollection", menuName ="SO Collection")]
public class ScriptableCollection : ScriptableObject
{
    public string selectedType = null;
    public string SelectedType => selectedType;

    public List<ScriptableObject> Objects = new();

    public void Create()
    {
        ScriptableObject NEWObject = Activator.CreateInstance(Type.GetType(selectedType)) as ScriptableObject;

        NEWObject.name = $"{Objects.Count}_NewObject";
        AssetDatabase.AddObjectToAsset(NEWObject, this);
        Undo.RegisterCreatedObjectUndo(NEWObject, "Added New Object");
        AssetDatabase.SaveAssets();
        Objects.Add(NEWObject);
    }
    public void DeleteAt(int i)
    {
        Undo.RecordObject(Objects[i], "Object Deleted");
        DestroyImmediate(Objects[i], true);
        Objects.RemoveAt(i);
        AssetDatabase.SaveAssets();
    }




}

[CustomEditor(typeof(ScriptableCollection), true)]
public class ScriptableCollectionEditor : Editor
{
    ScriptableCollection This;
    bool initialized;
    string TypeSetupString;

    void OnEnable()
    {
        This = target as ScriptableCollection;
        initialized = This.SelectedType != null;
        TypeSetupString = "Insert Type Name Here";
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        serializedObject.Update();

        EditorGUILayout.EditorToolbar();

        if (initialized)
        {
            for (int i = 0; i < This.Objects.Count; i++)
            {
                GUILayout.BeginHorizontal();

                string editedName = EditorGUILayout.DelayedTextField(This.Objects[i].name.Substring(2), GUILayout.ExpandWidth(true));
                if (editedName != This.Objects[i].name.Substring(2))
                {
                    This.Objects[i].name = $"{i}_{editedName}";
                    EditorUtility.SetDirty(This.Objects[i]);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("-", GUILayout.Width(30))) This.DeleteAt(i);
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("+")) This.Create();
            GUI.backgroundColor = Color.white;
        }
        else
        {
            TypeSetupString = GUILayout.TextField(TypeSetupString);
            if(GUILayout.Button("Initialize"))
            {
                var resultType = Type.GetType(TypeSetupString);
                This.selectedType = resultType != null ? resultType.AssemblyQualifiedName : throw new Exception("Invalid Type Name.");
                AssetDatabase.SaveAssetIfDirty(This);
                initialized = true;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    //private void Load() => assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(target)).Cast<ScriptableObject>().ToList();

}