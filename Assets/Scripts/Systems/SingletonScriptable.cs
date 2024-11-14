using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A global Singleton Scriptable Object. Must be added to Preloaded Assets in the Player Settings.
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingletonScriptable<T> : ScriptableObject where T : SingletonScriptable<T>
{
    private static T _instance;

    public static T Instance => Get();
    public static T I => Get();

    public static T Get() => _instance ? _instance : throw new System.Exception($"Scriptable Object Singleton {typeof(T)} is missing.");


    private void OnEnable()
    {
        if (_instance != null) return;
        _instance = this as T;
        _instance.OnAwake();
    }
    private void Awake()
    {
        if (_instance != null) return;
        _instance = this as T;
        _instance.OnAwake();
    }

    protected virtual void OnAwake() { }

    protected virtual void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}