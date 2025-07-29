using SLS.ISingleton;
using System;
using UnityEngine;
/// <summary>
/// A Singletonized version of the Menu base class. (The main reason I'm looking into Interface based Singletons for the future.)
/// </summary>
/// <typeparam name="T">The Type, should be the same as the class name.</typeparam>
public abstract class MenuSingleton<T> : Menu, ISingleton<T> where T : Menu, ISingleton<T>, new()
{
    protected static T Instance;
    protected ISingleton<T> Interface => this;
    public static T Get() => ISingleton<T>.Get(ref Instance);
    public static bool TryGet(out T result) => ISingleton<T>.TryGet(Get, out result);
    public static bool Loaded => Instance != null;
    public static bool Active => Loaded && Get().isActive;

    protected override void Awake()
    {
        Interface.Initialize(ref Instance);
        base.Awake();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Interface.DeInitialize(ref Instance);
    }

    //This redirection isn't necessary if creating the coding from scratch.
    //Just use the first two methods to override Initialization and DeInitialization functionality.
    void ISingleton<T>.OnInitialize() => OnInitialize();
    void ISingleton<T>.OnDeInitialize() => OnDeInitialize();
    protected virtual void OnInitialize() { }
    protected virtual void OnDeInitialize() { }
}
