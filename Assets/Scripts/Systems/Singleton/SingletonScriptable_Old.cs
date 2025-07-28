using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A global Singleton Scriptable Object. Must be added to Preloaded Assets in the Player Settings.
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingletonScriptable_Old<T> : _SingletonScriptableBase where T : SingletonScriptable_Old<T>
{
    private static T _instance;

    public static T Instance => Get();
    public static T I => Get();

    public static T Get() => _instance ?? throw new System.Exception($"Scriptable Object Singleton {typeof(T)} is missing.");


    protected sealed override void OnEnable()
    {
        if (_instance != null) return;
        _instance = this as T;
        _instance.OnAwake();
    }
    public sealed override void Awake()
    {
        if (_instance != null) return;
        _instance = this as T;
        _instance.OnAwake();
    }

    

    protected virtual void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

}

public abstract class _SingletonScriptableBase : ScriptableObject
{
    protected virtual void OnAwake() { }
    protected abstract void OnEnable();
    public abstract void Awake();
}

#if UNITY_EDITOR
class _SingletonScriptableAPP : UnityEditor.AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
        Object[] objs = UnityEditor.PlayerSettings.GetPreloadedAssets();
        foreach (Object item in objs)
        {
            if (item is _SingletonScriptableBase) (item as _SingletonScriptableBase).Awake();
        }
    }
}
#endif