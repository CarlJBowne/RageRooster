using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// A type of Behavior that can only exist once in a scene. <br/>
/// Further customization can be done with RuntimeInitializeOnLoadMethod, (See Bottom of script for example.)
/// </summary>
/// <typeparam name="T">The Behavior's Type</typeparam>
public abstract class Singleton<T> : Singleton where T : Singleton<T>
{
    #region Data and Setup

    private static T _instance;

    public static T Get() => GetDel?.Invoke();
    public static bool TryGet(out T output)
    {
        output = GetDel?.Invoke();
        return output != null;
    }

    public delegate T Delegate();
    protected static Delegate GetDel = InitFind;

    protected static void SetInfo(Delegate spawnMethod = null, bool dontDestroyOnLoad = false, bool spawnOnBoot = false, string path = null)
    {
        if (spawnMethod != null) GetDel = spawnMethod;
        if (path != null) Path = path;
        DontDestoryOnLoad = dontDestroyOnLoad;
        if (spawnMethod == InitSavedPrefab)
        {
            Singleton S = GlobalPrefabs.Get().singletons.FirstOrDefault(x => x is T);
            prefab = S
                ? S.gameObject
                : throw new Exception($"Singleton {typeof(T)} is labeled as using a saved prefab but isn't set up in the Global Prefabs Asset.");
        }

        if (spawnOnBoot) spawnMethod?.Invoke();
    }
    /// <summary>
    /// Override to make this Singleton not destroy on load.
    /// </summary>
    static bool DontDestoryOnLoad = false;
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
    protected static T InitFind()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't."); return null; }
#endif

        if (_instance != null) return _instance;
        if (AttemptFind(out T attempt))
        {
            InitFinal(attempt);
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

        InitFinal(result);
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

        InitFinal(result.GetComponent<T>());
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

        InitFinal(result.GetComponent<T>());
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
    protected static void InitFinal(T input)
    {
        if (_instance || _instance == input) return;
        _instance = input;
        if (DontDestoryOnLoad) DontDestroyOnLoad(_instance);
        _instance.OnAwake();
    }

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
            InitFinal(this as T);
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
        }
        OnDestroyed();
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

public abstract class Singleton : MonoBehaviour { };


/* Example Use --------------------------------------------------------------------------------------------------------------------------------------------

public class ExampleSingleton : Singleton<ExampleSingleton>
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() => SetInfo(spawnMethod: InitResourcePrefab, dontDestroyOnLoad: true, spawnOnBoot: true, path: "ExampleSingleton");
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