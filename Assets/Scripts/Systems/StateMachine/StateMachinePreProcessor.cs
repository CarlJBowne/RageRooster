#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Collections.Generic;
using SLS.StateMachineV3;

public class BuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        EditorState.OnBuildStateMachineHandling setting = EditorState.Get().onBuildStateMachineHandling;
        if (setting is EditorState.OnBuildStateMachineHandling.DoNothing) return;

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null) continue;

            // Open prefab contents as a temporary scene-like instance
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);

            if(prefabInstance.TryGetComponent(out StateMachine SM))
            {
                if (setting == EditorState.OnBuildStateMachineHandling.SetupIfNotSetup && !SM.statesSetup)
                {
                    SM.Setup(SM, SM, -1);
                }
                else if (setting == EditorState.OnBuildStateMachineHandling.SetupIfNotSetupAndSave && !SM.statesSetup)
                {
                    SM.Setup(SM, SM, -1, true);
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
                }
                else if (setting == EditorState.OnBuildStateMachineHandling.SetupRegardless)
                {
                    SM.Setup(SM, SM, -1);
                }
                else if (setting == EditorState.OnBuildStateMachineHandling.SetupRegardlessAndSave)
                {
                    SM.Setup(SM, SM, -1, true);
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
                }
            }
            PrefabUtility.UnloadPrefabContents(prefabInstance);
        }

        if(setting is EditorState.OnBuildStateMachineHandling.SetupIfNotSetupAndSave or EditorState.OnBuildStateMachineHandling.SetupRegardlessAndSave)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    //private static List<StateMachine> FindAllPrefabInstances<T>() where T : MonoBehaviour
    //{
    //    var results = new List<StateMachine>();
    //    string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
    //
    //    foreach (string guid in prefabGuids)
    //    {
    //        string path = AssetDatabase.GUIDToAssetPath(guid);
    //        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    //        if (prefab == null) continue;
    //
    //        // Load components of type T from prefab
    //        var components = prefab.GetComponentsInChildren<StateMachine>(true);
    //        if (components.Length > 0)
    //            results.AddRange(components);
    //    }
    //
    //    return results;
    //}
}
#endif