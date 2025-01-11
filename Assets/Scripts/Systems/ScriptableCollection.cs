using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSOCollection", menuName ="SO Collection"), System.Serializable]
public class ScriptableCollection : ScriptableObject, ICustomSerialized
{
    public string selectedType = null;
    public string SelectedType => selectedType;

    public List<ScriptableObject> List = new();

    public void Create()
    {
        ScriptableObject NEWObject = Activator.CreateInstance(Type.GetType(selectedType)) as ScriptableObject;

        NEWObject.name = $"{List.Count}_NewObject";
        AssetDatabase.AddObjectToAsset(NEWObject, this);
        Undo.RegisterCreatedObjectUndo(NEWObject, "Added New Object");
        AssetDatabase.SaveAssets();
        List.Add(NEWObject);
    }
    public void DeleteAt(int i)
    {
        Undo.RecordObject(List[i], "Object Deleted");
        DestroyImmediate(List[i], true);
        List.RemoveAt(i);
        AssetDatabase.SaveAssets();
    }

    public Json Serialize()
    {
        Json.Builder Build = new();
        Build.AddString(nameof(selectedType), selectedType);
        Build.AddList(nameof(List), List, true);
        return Build.Result();
    }
    public void Deserialize(Json Data)
    {
        Dictionary<string, object> jsonData = Data.DeserializeAll();

        string readSOType = jsonData[nameof(selectedType)] as string;
        if (readSOType != selectedType) throw new Exception("WRONG SCRIPTABLE OBJECT TYPE.");
        var jsonArray = jsonData[nameof(List)] as Newtonsoft.Json.Linq.JArray;

        for (int i = 0; i < jsonArray.Count; i++)
        {
            if (i >= List.Count) throw new Exception("JSON contains more items than the current collection.");
            Json.DeserializeInto(jsonArray[i], List[i]);
            EditorUtility.SetDirty(List[i]);
        }
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
            for (int i = 0; i < This.List.Count; i++)
            {
                GUILayout.BeginHorizontal();

                string editedName = EditorGUILayout.DelayedTextField(This.List[i].name.Substring(2), GUILayout.ExpandWidth(true));
                if (editedName != This.List[i].name.Substring(2))
                {
                    This.List[i].name = $"{i}_{editedName}";
                    EditorUtility.SetDirty(This.List[i]);
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

