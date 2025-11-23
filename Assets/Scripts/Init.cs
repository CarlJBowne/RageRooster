using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class Init
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        AudioManager.Get();
        Debug.Log("Project initialized successfully.");
    }
}