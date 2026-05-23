 
using System;
using UnityEngine;
using Utilities.Singletons;

/// <summary>
/// A Singletonized version of the Menu base class. (The main reason I'm looking into Interface based Singletons for the future.)
/// </summary>
/// <typeparam name="T">The Type, should be the same as the class name.</typeparam>
public abstract class MenuSingleton<T> : Menu where T : Menu
{
    protected static T instance;
    public static T Get => Singleton.Get(ref instance);
    public static bool TryGet(out T result) => Singleton.TryGet(Get, out result);
    public static bool Loaded => instance != null;
    public static bool Active => Loaded && Get.isActive;

    protected sealed override void Awake()
    {
        Singleton.Register<T>(ref instance, this as T);
        base.Awake();
        OnInitialize();
    }
    protected sealed override void OnDestroy()
    {
        base.OnDestroy();
        Singleton.Deregister<T>(ref instance, this as T);
        OnDeInitialize();
    }

    protected virtual void OnInitialize() { }
    protected virtual void OnDeInitialize() { }
}
