using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestSingleton : Singleton<TestSingleton>
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AAAA()
    {
        prefab = FindFirstObjectByType<TestSingleton>(FindObjectsInactive.Include).gameObject;
        Instantiate(prefab);
    }
    public static GameObject prefab;


    protected override void OnAwake()
    {
        Debug.Log("Success");
    }
}