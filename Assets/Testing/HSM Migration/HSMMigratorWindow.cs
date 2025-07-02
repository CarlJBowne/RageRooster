using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using OLD = SLS.StateMachineV3;
using NEW = SLS.StateMachineH;
using Unity.VisualScripting;

public class HSMMigratorWindow : EditorWindow
{

    private HSMMigrationMidData data;

    [MenuItem("Window/HSMMigrator")]
    public static void ShowWindow()
    {
        GetWindow<HSMMigratorWindow>("HSMMigrator");
    }
    private void OnGUI()
    {
        if (data == null)
        {
            string assetPath = "Assets/Testing/HSM Migration/MidData.asset";
            data = AssetDatabase.LoadAssetAtPath<HSMMigrationMidData>(assetPath);

            if (data == null)
            {
                HSMMigrationMidData midData = ScriptableObject.CreateInstance<HSMMigrationMidData>();
                AssetDatabase.CreateAsset(midData, assetPath);
                AssetDatabase.SaveAssets();
                data = midData;
            }
        }

        if (data.workingMachines == null || data.workingMachines.Count == 0)
        {
            if (GUILayout.Button("Run Step0")) Step0();
        }
        else
        {
            foreach (var machine in data.workingMachines)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(machine.readOnlyObject.name, GUILayout.Width(200)))
                {
                    EditorGUIUtility.PingObject(machine.readOnlyObject);
                }

                GUILayout.Label($"Phase: {machine.phase}", GUILayout.Width(100));

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Run Step1")) Step1();
            if (GUILayout.Button("Run Step2")) Step2();
            if (GUILayout.Button("Run Step4")) Step4();
            if (GUILayout.Button("Run Step5")) Step5();
            if (GUILayout.Button("Run Step6")) Step6();
            if (GUILayout.Button("RemoveDupes")) RemoveDupes();
        }
    }

    public void Step0()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
        {
            HSMMachine prefab = new(AssetDatabase.GUIDToAssetPath(guid));

            if (prefab.readOnlyObject == null || !prefab.readOnlyObject.TryGetComponent(out OLD.StateMachine Machine)) continue;

            data.workingMachines.Add(prefab);

            prefab.Open();

            Machine.ManualSetup();

            AddHelper(prefab.editableObject.GetComponent<OLD.StateMachine>());

            prefab.Close();
            EditorUtility.SetDirty(data);
        }


    }


    public HSMMigrationHelper AddHelper(OLD.State state)
    {
        HSMMigrationHelper result = state.gameObject.GetOrAddComponent<HSMMigrationHelper>();
        if (result == null) return null;

        result.oldState = state;
        if(state.children != null)
            foreach (OLD.State child in state.children)
            {
                var cHelper = AddHelper(child);
                if(!result.children.Contains(cHelper)) result.children.Add(cHelper);
            }
                

        EditorUtility.SetDirty(state.gameObject);
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(result);

        return result;
    }

    public void Step1()
    {
        foreach (var item in data.workingMachines)
        {
            item.Open();

            item.editableObject.GetComponent<HSMMigrationHelper>().DoStep1(true);

            item.phase = 1;

            item.Close();
        }
    }
    public void Step2()
    {
        foreach (var item in data.workingMachines)
        {
            item.Open();

            item.editableObject.GetComponent<HSMMigrationHelper>().DoStep2(true);

            item.phase = 2;

            item.Close();
        }
    }
    public void Step4()
    {
        foreach (var item in data.workingMachines)
        {
            item.Open();

            item.editableObject.GetComponent<HSMMigrationHelper>().DoStep4(true);

            item.phase = 4;

            item.Close();
        }
    }
    public void Step5()
    {
        foreach (var item in data.workingMachines)
        {
            item.Open();

            item.editableObject.GetComponent<HSMMigrationHelper>().DoStep5(true);

            item.phase = 5;

            item.Close();
        }
    }
    public void Step6()
    {
        foreach (var item in data.workingMachines)
        {
            item.Open();

            item.editableObject.GetComponent<HSMMigrationHelper>().DoStep6(true);

            item.phase = 6;

            item.Close();
        }
    }

    public void RemoveDupes()
    {
        foreach (var item in data.workingMachines)
        {
            item.Open();
            item.editableObject.GetComponent<HSMMigrationHelper>().RemoveDupes();
            item.Close();
        }
    }


}
