using System;
using UnityEngine;
/// <summary>
/// A Singletonized version of the Menu base class. (The main reason I'm looking into Interface based Singletons for the future.)
/// </summary>
/// <typeparam name="T">The Type, should be the same as the class name.</typeparam>
public abstract class MenuSingleton<T> : Menu where T : MenuSingleton<T>
{
    private static T _instance;
    public static bool Loaded;

    /// <summary>
    /// Gets the singleton instance
    /// </summary>
    public static T Get() => InitFind();

    /// <summary>
    /// Gets the singleton instance
    /// </summary>
    public static T I => InitFind();

    /// <summary>
    /// Tries to get the singleton instance
    /// </summary>
    /// <param name="output"></param>
    public static bool TryGet(out T output)
    {
        output = InitFind();
        return output != null;
    }

    /// <summary>
    /// Checks if the singleton instance is active
    /// </summary>
    /// <param name="output"></param>
    public static bool IsActive(out T output)
    {
        output = InitFind();
        return output != null;
    }

    /// <summary>
    /// Gets the singleton instance or throws an exception if not found
    /// </summary>
    /// <param name="output"></param>
    /// <exception cref="Exception"></exception>
    public static void Get(out T output)
    {
        output = InitFind();
        if (output == null) throw new Exception("Singleton not active.");
    }

    /// <summary>
    /// Checks if the singleton instance is active
    /// </summary>
    public static bool Active => Get().isActive;

    /// <summary>
    /// Initializes and finds the singleton instance
    /// </summary>
    protected static T InitFind()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't.");
            return null;
        }
#endif

        if (_instance != null) return _instance;
        if (AttemptFind(out T attempt))
        {
            attempt.GetComponent<T>().Awake();
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

    /// <summary>
    /// Attempts to find the singleton instance
    /// </summary>
    /// <param name="result"></param>
    protected static bool AttemptFind(out T result)
    {
        T findAttempt = FindFirstObjectByType<T>();
        if (findAttempt)
        {
            result = findAttempt;
            _instance = result;

            _instance.Awake();
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Called when the script instance is being loaded
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        if (Loaded) return;
        if (_instance || _instance == this) return;
        _instance = this as T;
        Loaded = true;
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Loaded = false;
        _instance = null;
    }
}
