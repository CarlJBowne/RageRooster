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
        if (GUILayout.Button("LoadPrefabs")) LoadPrefabs(true);

        if (loadedPrefabs != null && loadedPrefabs.Count > 0)
        {
            foreach (var item in loadedPrefabs)
            {
                GUIStyle linkStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.blue },
                    hover = { textColor = Color.cyan },
                    fontStyle = FontStyle.Bold
                };

                if (GUILayout.Button(item.readOnlyObject.name, linkStyle, GUILayout.Width(200)))
                {
                    EditorGUIUtility.PingObject(item.readOnlyObject);
                }
            }
        }

        Button("AddMissingStates", AddMissingStates, "Does on all Prefabs.");
        Button("DealWithSeparates", DealWithSeparates, "Does on all Prefabs.");
        Button("BuildALL", BuildAll, "Does on all Prefabs.");
        Button("ReplaceSignals", ReplaceSignals, "Does on all Prefabs.");
        Button("AddMissingStatesOnSelection", AddMissingStatesOnSelection, "Does on Selected Objects.");

    }

    List<Prefab> loadedPrefabs;

    public void Button(string name, BasicDelegate action, string note)
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(name)) action();
        GUILayout.Label(note);

        EditorGUILayout.EndHorizontal();
    }

    void LoadPrefabs(bool doAnyway = false)
    {
        if ((loadedPrefabs != null && loadedPrefabs.Count > 0) && !doAnyway) return;

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        loadedPrefabs = new();

        foreach (string guid in prefabGuids)
        {
            Prefab prefab = new(AssetDatabase.GUIDToAssetPath(guid));

            if (prefab.readOnlyObject == null || 
                (!prefab.readOnlyObject.TryGetComponent(out OLD.StateMachine_OLD Machine) &&
                !prefab.readOnlyObject.TryGetComponent(out PlayerStateMachine PMachine))
                ) continue;

            loadedPrefabs.Add(prefab);
        }
    }

    public void AddMissingStates()
    {
        LoadPrefabs();

        foreach (var p in loadedPrefabs)
        {
            p.Open();

            var Root = p.editableObject.transform.Find("States");

            AddMissingState(Root, true);
            void AddMissingState(Transform This, bool root = false)
            {
                if (!This.TryGetComponent(out NEW.State _) && !root)
                {
                    var NEW = This.AddComponent<NEW.State>();
                    EditorUtility.SetDirty(This.gameObject);
                    EditorUtility.SetDirty(NEW);
                }

                for (int i = 0; i < This.childCount; i++)
                    AddMissingState(This.GetChild(0));
            }
            p.Close();
        }
    }


    public void DealWithSeparates()
    {
        LoadPrefabs();

        foreach (var p in loadedPrefabs)
        {
            p.Open();

            var Root = p.editableObject.transform.Find("States");

            DealWithSeparate(Root, true);

            p.Close();
        }
    }

    void DealWithSeparate(Transform This, bool root = false)
    {
        bool found = This.TryGetComponent(out OLD.State_OLD state);

        if (!root && found && state.separateFromChildren && This.childCount > 0)
        {
            var defChild = new GameObject("Default");
            defChild.transform.SetParent(This, false);
            defChild.transform.SetSiblingIndex(0);
            var defStateOld = defChild.AddComponent<OLD.State_OLD>();
            var defStateNew = defChild.AddComponent<NEW.State>();

            defStateOld.signals = state.signals;
            state.signals = new();

            EditorUtility.SetDirty(This);
            EditorUtility.SetDirty(defChild);
            EditorUtility.SetDirty(defStateOld);
            EditorUtility.SetDirty(defStateNew);
            EditorUtility.SetDirty(state);

        }

        for (int i = 0; i < This.childCount; i++)
            DealWithSeparate(This.GetChild(i));
    }

    void BuildAll()
    {
        LoadPrefabs();

        foreach (var p in loadedPrefabs)
        {
            p.Open();

            if(p.editableObject.TryGetComponent(out NEW.StateMachine SM))
            {
                SM.Build();
                EditorUtility.SetDirty(SM);
            }

            p.Close();
        }
    }

    public void ReplaceSignals()
    {
        LoadPrefabs();
        foreach (var p in loadedPrefabs)
        {
            p.Open();

            var SigMan = p.editableObject.GetOrAddComponent<NEW.SignalManager>();
            if(p.editableObject.TryGetComponent(out OLD.StateMachine_OLD SM) && SM.globalSignals != null && SM.globalSignals.Count >0)
                SigMan.globalSignals = ConvertFromYellowPaper(SM.globalSignals);
            EditorUtility.SetDirty(p.editableObject);
            EditorUtility.SetDirty(SigMan);

            var Root = p.editableObject.transform.Find("States");

            ReplaceSignal(Root, true);
            void ReplaceSignal(Transform This, bool root = false)
            {
                if (!root && This.TryGetComponent(out OLD.State_OLD oldState) && oldState.signals.Count != 0)
                {
                    var SN = This.GetOrAddComponent<NEW.SignalNode>();
                    SN.signals = ConvertFromYellowPaper(oldState.signals);

                    EditorUtility.SetDirty(This);
                    EditorUtility.SetDirty(SN);
                }

                for (int i = 0; i < This.childCount; i++)
                    ReplaceSignal(This.GetChild(i));
            }

            p.Close();
        }
    }

    private NEW.SignalSet ConvertFromYellowPaper(AYellowpaper.SerializedCollections.SerializedDictionary<string, UltEvents.UltEvent> input)
    {
        if(input == null || input.Count == 0) return new NEW.SignalSet();
        var output = new NEW.SignalSet();
        foreach (var pair in input)
        {
            output.Add(pair.Key, pair.Value);
        }
        return output;

    }

    public void AddMissingStatesOnSelection()
    {
        //Loop through objects selected in hierarchy
        foreach (GameObject obj in Selection.gameObjects)
        {
            obj.TryGetComponent(out NEW.State exists);
            if (!exists)
            {
                EditorUtility.SetDirty(obj.AddComponent<NEW.State>());
                EditorUtility.SetDirty(obj);
            }
        }
    }











    #region Version 1
    private void Version1()
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
            if (GUILayout.Button("FixMissings")) FixMissings();
        }
    }

    public void Step0()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
        {
            HSMMachine prefab = new(AssetDatabase.GUIDToAssetPath(guid));

            if (prefab.readOnlyObject == null || !prefab.readOnlyObject.TryGetComponent(out OLD.StateMachine_OLD Machine)) continue;

            data.workingMachines.Add(prefab);

            prefab.Open();

            Machine.ManualSetup();

            AddHelper(prefab.editableObject.GetComponent<OLD.StateMachine_OLD>());

            prefab.Close();
            EditorUtility.SetDirty(data);
        }


    }


    public static HSMMigrationHelper AddHelper(OLD.State_OLD state)
    {
        HSMMigrationHelper result = state.gameObject.GetOrAddComponent<HSMMigrationHelper>();
        if (result == null) return null;

        result.oldState = state;
        if(state.children != null)
            foreach (OLD.State_OLD child in state.children)
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
    public void FixMissings()
    {
        foreach (var item in data.workingMachines)
        {
            item.Open();
            item.editableObject.GetComponent<HSMMigrationHelper>().FixMissings();
            item.Close();
        }
    }

    #endregion
}
