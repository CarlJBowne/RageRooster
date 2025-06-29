using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using OLD = SLS.StateMachineV3;
using NEW = SLS.StateMachineH;

public class HSMMigratorWindow : EditorWindow
{
    [MenuItem("Window/HSMMigrator")]
    public static void ShowWindow()
    {
        GetWindow<HSMMigratorWindow>("HSMMigrator");
    }
    private void OnGUI()
    {














    }

    public void Step0()
    {
        CreateSO();
        ObtainMachines();
        foreach (GameObject machine in savedData.machines)
            savedData.helpers.Add(machine.GetComponent<OLD.StateMachine>().AddMigrationHelper());
    }

    private void CreateSO()
    {
        HSMMigrationMidData midData = ScriptableObject.CreateInstance<HSMMigrationMidData>();
        AssetDatabase.CreateAsset(midData, "Assets/Testing/HSM Migration/MidData.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        savedData = midData;
    }
    private HSMMigrationMidData savedData;


    public void ObtainMachines()
    {
        var result = new List<GameObject>();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || !prefab.TryGetComponent(out OLD.StateMachine _)) continue;
            result.Add(prefab);
        }
        savedData.machines = result;
    }

    public HSMMigrationHelper AddHelper(OLD.State state)
    {
        HSMMigrationHelper result = state.AddMigrationHelper();
        
        if (result == null) return null;
        foreach (OLD.State child in state.children)
            result.children.Add(AddHelper(child));

        return result;
    }

    public void Step1()
    {
        foreach (var machine in savedData.machines) 
            machine.GetComponent<HSMMigrationHelper>().AddNewVersion();
    }






}
