using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/// <summary>
/// A type of Behavior that can only exist once in a scene. <br/>
/// Basic form that functions out of the box. (Inheret from SingletonAdvanced instead for special functionality.)
/// </summary>
/// <typeparam name="T">The Behavior's Type</typeparam>
public abstract class Singleton<T> : SingletonAncestor where T : Singleton<T>
{
    #region Data and Setup

    private static T _instance;

    public static T Get() => InitFind();
    public static T I => InitFind();
    public static bool TryGet(out T output)
    {
        output = InitFind();
        return output != null;
    }
    public static bool IsActive(out T output)
    {
        output = InitFind();
        return output != null;
    }
    public static void Get(out T output)
    {
        output = InitFind();
        if (output != null) throw new Exception("Singleton not active.");
    }


    public static bool Active;

    #endregion

    #region Initialization

    protected static bool AttemptFind(out T result)
    {
        T findAttempt = FindFirstObjectByType<T>();
        if (findAttempt)
        {
            result = findAttempt;
            _instance = result;

            _instance.OnAwake();
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Simply Attempts to Find the Singleton in the current scene.
    /// </summary>
    /// <returns></returns>
    protected static T InitFind()
    {
        #if UNITY_EDITOR
        if (!Application.isPlaying) { Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't."); return null; }
        #endif

        if (_instance != null) return _instance;
        if (AttemptFind(out T attempt))
        {
            attempt.GetComponent<T>().InitFinal();
            return _instance;
        }
        else
        {
            #if UNITY_EDITOR
            Debug.LogError($"No Singleton of type {typeof(T)} could be found.");
            #endif
            return null;
        }
    }

    protected void InitFinal()
    {
        if (_instance || _instance == this) return;
        _instance = this as T;
        Active = true;
        activeSingletons.Add(typeof(T), this);
        if (DontDestoryOnLoad) DontDestroyOnLoad(_instance);
        _instance.OnAwake();
    }

    protected static bool DontDestoryOnLoad = false;

    #endregion

    #region Other Functionality


    /// <summary>
    /// This is the Unity Function which runs some code necessary for Singleton Function. Use OnAwake() instead.
    /// </summary>
    public void Awake()
    {
        if (_instance && _instance != this)
        {
#if UNITY_EDITOR
            Debug.Log($"Second {typeof(T)} found, Destroying...");
#endif

            Destroy(this);
        }
        else
        {
            if (_instance == this) return;
            (this as T).InitFinal(); ;
        }
    }

    protected virtual void OnAwake() { }

    /// <summary>
    /// This is the Unity Function which runs some code necessary for Singleton Function. Use OnDestroyed() instead.
    /// </summary>
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            Active = false;
            activeSingletons.Remove(typeof(T));
        }
        this.OnDestroyed();
    }
    protected virtual void OnDestroyed() { }

    /// <summary>
    /// Destroys the instance of this singleton, wherever it is.
    /// </summary>
    /// <param name="leaveGameObject"> Whether the Game Object that contains the Singleton is left behind.</param>
    public static void DestroyS(bool leaveGameObject = false)
    {
        if (_instance == null) return;
        if (!leaveGameObject)
        {
            Destroy(_instance.gameObject);
        }
        else
        {
            Destroy(_instance);
            _instance.OnDestroy();
        }
    }

    /// <summary>
    /// Very Dangerous. Do not use if you don't know what you're doing.
    /// </summary>
    public void Reset(bool ResetWholeGameObject)
    {
        if (ResetWholeGameObject)
        {
            GameObject obj = _instance.gameObject;
            DestroyS(true);
            obj.AddComponent<T>();
        }
        else
        {
            DestroyS(false);
            Get();
        }

    }

    #endregion

}

/// <summary>
/// A type of Behavior that can only exist once in a scene. <br/>
/// Advanced form that can be customized with a static "Data" method. (See bottom of script file for examples.)
/// </summary>
/// <typeparam name="T">The Behavior's Type</typeparam>
public abstract class SingletonAdvanced<T> : Singleton<SingletonAdvanced<T>> where T : SingletonAdvanced<T>
{
    #region Data and Setup

    private static T _instance;

    public new static T Get() => GetDel?.Invoke();
    public new static T I => GetDel?.Invoke();
    public static bool TryGet(out T output)
    {
        output = GetDel?.Invoke();
        return output != null;
    }
    public static bool IsActive(out T output)
    {
        output = GetDel?.Invoke();
        return output != null;
    }
    public static void Get(out T output)
    {
        output = InitFind();
        if (output != null) throw new Exception("Singleton not active.");
    }



    public delegate T Delegate();
    protected static Delegate GetDel = InitFind;

    protected static void SetData(Delegate spawnMethod = null, bool dontDestroyOnLoad = false, bool spawnOnBoot = false, string path = null)
    {
        if (spawnMethod != null) GetDel = spawnMethod;
        if (path != null) Path = path;
        DontDestoryOnLoad = dontDestroyOnLoad;
        if (spawnMethod == InitSavedPrefab)
        {
            SingletonAncestor S = GlobalPrefabs.Singletons.FirstOrDefault(x => x is T);
            prefab = S
                ? S.gameObject
                : throw new Exception($"Singleton {typeof(T)} is set to use a saved prefab but isn't set up in the Global Prefabs Asset.");
        }

        if (spawnOnBoot) spawnMethod?.Invoke();
    }
    /// <summary>
    /// Override to make this Singleton not destroy on load.
    /// </summary>
    protected static string Path = null;
    protected static GameObject prefab;

    #endregion

    #region Initialization

    protected static bool AttemptFind(out T result)
    {
        T findAttempt = FindFirstObjectByType<T>();
        if (findAttempt)
        {
            result = findAttempt;
            _instance = result;

            _instance.OnAwake();
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Simply Attempts to Find the Singleton in the current scene.
    /// </summary>
    /// <returns></returns>
    protected new static T InitFind()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't."); return null; }
#endif

        if (_instance != null) return _instance;
        if (AttemptFind(out T attempt))
        {
            attempt.GetComponent<T>().InitFinal();
            return _instance;
        }

#if UNITY_EDITOR
        Debug.LogError($"No Singleton of type {typeof(T)} could be found.");
#endif        
        return null;
    }

    /// <summary>
    /// Creates an instance of the Singleton from scratch.
    /// </summary>
    /// <returns></returns>
    protected static T InitCreate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't."); return null; }
#endif

        if (_instance != null) return _instance;
        if (AttemptFind(out T attempt)) return attempt;

        GameObject GO = new(typeof(T).ToString());
        T result = GO.AddComponent<T>();

        result.GetComponent<T>().InitFinal();
        return result;
    }

    /// <summary>
    /// Instantiates a Prefab from the Resources folder. (Make sure to set the path in SetInfo.)
    /// </summary>
    /// <returns></returns>
    protected static T InitResourcePrefab()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't."); return null; }
#endif

        if (_instance != null) return _instance;
        if (AttemptFind(out T attempt)) return attempt;

        GameObject result = Instantiate(Resources.Load<GameObject>(Path));

        result.GetComponent<T>().InitFinal();
        return _instance;
    }

    /// <summary>
    /// Instantiates a Prefab from the GlobalPrefabs ScriptableSingleton. (Make sure to place exactly one prefab into said Scriptable Object.)
    /// </summary>
    /// <returns></returns>
    protected static T InitSavedPrefab()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't."); return null; }
#endif

        if (_instance != null) return _instance;
        if (AttemptFind(out T attempt)) return attempt;

        if (!prefab)
        {
            Debug.LogError("");
            return null;
        }
        GameObject result = Instantiate(prefab);

        result.GetComponent<T>().InitFinal();
        return _instance;
    }

#if UNITY_ADDRESSABLES_EXIST
    /// <summary>
    /// Instantiates a Prefab using the Addressables System. (Make sure to set the path in SetInfo.)
    /// </summary>
    /// <returns></returns>
    protected static T InitAddressablePrefab()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't."); return null; }
#endif

        if (_instance != null) return _instance;
        if (AttemptFind(out T attempt)) return attempt;

        GameObject result = Instantiate(UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(Path).WaitForCompletion());

        InitFinal(result.GetComponent<T>());
        return _instance;
    }
#endif

    #endregion

    /// <summary>
    /// This is the Unity Function which runs some code necessary for Singleton Function. Use OnDestroyed() instead.
    /// </summary>
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            activeSingletons.Remove(typeof(T));
        }
        this.OnDestroyed();
    }

}


public abstract class SingletonAncestor : MonoBehaviour
{
    public static Dictionary<Type, SingletonAncestor> activeSingletons = new();
    public static T Get<T>() where T : SingletonAncestor => activeSingletons[typeof(T)] as T;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Boot()
    {
#if UNITY_EDITOR
        Debug.Log("Loading Singletons");
#endif
        foreach (Type item in Assembly.GetAssembly(typeof(SingletonAdvanced<>)).GetTypes().Where(t => typeof(SingletonAdvanced<>).IsAssignableFrom(t) && !t.IsAbstract))
        {
            MethodInfo M = item.GetMethod("Data", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
            M?.Invoke(null, null);
            /*
                var F1 = item.GetField("InitMethod", BindingFlags.Static | BindingFlags.Public);
                var F2 = item.GetField("DontDestroyOnLoad", BindingFlags.Static | BindingFlags.Public);
                var F3 = item.GetField("Path", BindingFlags.Static | BindingFlags.Public);
                var F4 = item.GetField("SpawnOnBoot", BindingFlags.Static | BindingFlags.Public);

                var M = item.BaseType.GetMethod("SetInfo", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
                    M.Invoke(null, new[]{
                        F1 != null ? F1.GetValue(null) : null, 
                        F2 != null ? F2.GetValue(null) : false, 
                        F4 != null ? F4.GetValue(null) : false,  
                        F3 != null ? F3.GetValue(null) : null});
            */
            /*
                //var F2 = item.GetField("DontDestroyOnLoad", BindingFlags.Static | BindingFlags.Public);
                //if (F2 != null) item.GetField("_DontDestroyOnLoad", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, F2);
                //var F3 = item.GetField("Path", BindingFlags.Static | BindingFlags.Public);
                //if (F3 != null) item.GetField("_Path", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, F3);
                //var F1 = item.GetField("InitMethod", BindingFlags.Static | BindingFlags.Public);
                //if(F1 != null) item.GetField("_GetDel", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, F1);
            */
        }
    }

}

/* Example Use --------------------------------------------------------------------------------------------------------------------------------------------

public class ExampleSingleton : SingletonAdvanced<ExampleSingleton>
{
    static void Data() => SetData(spawnMethod: InitResourcePrefab, dontDestroyOnLoad: true, spawnOnBoot: true, path: "ExampleSingleton");
}

Spawn methods include:

InitFind
--------Simply Attempts to Find the Singleton in the current scene.

InitCreate
----------Creates an instance of the Singleton from scratch.

InitResourcePrefab
------------------Instantiates a Prefab from the Resources folder. (Make sure to set the path in SetInfo.)

InitSavedPrefab
---------------Instantiates a Prefab from the GlobalPrefabs ScriptableSingleton. (Make sure to place exactly one prefab into said Scriptable Object.)

InitAddressablePrefab
---------------------Instantiates a Prefab using the Addressables System. (Make sure to set the path in SetInfo.)



 */