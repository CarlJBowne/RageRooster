using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class TestSingleton : Singleton<TestSingleton>
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() => SetInfo(InitSavedPrefab, true, true);


    protected override void OnAwake()
    {
        Debug.Log("Success");
    }

    public string a = "AAA";
}