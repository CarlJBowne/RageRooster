using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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
        List<GameObject> machines = ObtainMachines();
        AddMachineHelpers(machines);
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


    public List<GameObject> ObtainMachines()
    {
        var result = new List<GameObject>();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var component = prefab.GetComponent<SLS.StateMachineH.StateMachine>();
            if (component == null) continue;
            result.Add(prefab);
        }
        return result;
    }

    public void AddMachineHelpers(List<GameObject> prefabs)
    {
        foreach (var prefab in prefabs)
        {
            HSMMigrationHelper helper = prefab.GetOrAddComponent<HSMMigrationHelper>();
            helper.oldStateMachine = prefab.GetComponent<SLS.StateMachineV3.StateMachine>();
            helper.AddNewVersion();
            savedData.machines.Add(helper);
        }
        EditorUtility.SetDirty(savedData);
        AssetDatabase.SaveAssets();
    }

    public void AddStateHelper(SLS.StateMachineV3.State state)
    {
        HSMMigrationHelper helper = state.GetOrAddComponent<HSMMigrationHelper>();
        helper.

        EditorUtility.SetDirty(savedData);
        AssetDatabase.SaveAssets();
    }









}
