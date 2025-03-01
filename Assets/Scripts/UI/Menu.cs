using EditorAttributes;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A Base Menu class intended to form an easy-to-use extensible Menu system. (Admittedly not up to snuff.)
/// </summary>
public class Menu : MonoBehaviour
{
    //Config
    [DisableInPlayMode] public bool isActive;
    [DisableInPlayMode] public Menu parent;
    [SerializeField] private string dictionaryName;
    [SerializeField] private bool closeOverride;
    [SerializeField, ShowField(nameof(closeOverride))] private UnityEvent closeEvent;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference closeSound;

    //Data
    public bool isCurrent => Manager.currentMenu == this;
    public bool isSubMenu => parent != null;

    /// <summary>
    /// Called when the script instance is being loaded
    /// </summary>
    protected virtual void Awake()
    {
        if (isActive) Manager.Open(this);
        else
        {
            gameObject.SetActive(false);
        }

        if (!string.IsNullOrEmpty(dictionaryName)) Manager.menuDictionary.Add(dictionaryName, this);
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (!string.IsNullOrEmpty(dictionaryName)) Manager.menuDictionary.Remove(dictionaryName);
        isActive = false;
        Manager.Close(this);
    }

    /// <summary>
    /// Opens the menu
    /// </summary>
    public void Open() => Manager.Open(this);

    /// <summary>
    /// Closes the menu (Invokes Override if present.)
    /// </summary>
    public void Close()
    {
        if (!closeOverride) Manager.Close(this);
        else closeEvent?.Invoke();
    }

    /// <summary>
    /// Closes the menu (Closes even if Override is present.)
    /// </summary>
    public void TrueClose() => Manager.Close(this);

    /// <summary>
    /// Called when the menu is opened
    /// </summary>
    protected virtual void OnOpen()
    {
         if (!openSound.IsNull)
             AudioManager.Get().PlayOneShot(openSound, transform.position);
    }

    /// <summary>
    /// Called when the menu is closed
    /// </summary>
    protected virtual void OnClose()
    {
        if (!closeSound.IsNull)
            AudioManager.Get().PlayOneShot(closeSound, transform.position);
    }

    /// <summary>
    /// The Global Manager in charge of Menus.
    /// </summary>
    public static class Manager
    {
        public static Menu currentMenu => currentMenus[^1];
        public static List<Menu> currentMenus = new();
        public static Dictionary<string, Menu> menuDictionary = new();
        public static bool disableEscape;

        /// <summary>
        /// Opens the specified menu
        /// </summary>
        /// <param name="menu">The Menu to be opened.</param>
        public static void Open(Menu menu)
        {
            if (menu.isActive) return;

            currentMenus.Add(menu);

            menu.isActive = true;
            menu.gameObject.SetActive(true);
            menu.OnOpen();
        }

        /// <summary>
        /// Closes the specified menu
        /// </summary>
        /// <param name="menu">The Menu to be closed.</param>
        public static void Close(Menu menu)
        {
            if (!menu.isActive) return;

            currentMenus.Remove(menu);

            menu.gameObject.SetActive(false);
            menu.isActive = false;
            menu.OnClose();
        }

        /// <summary>
        /// Handles the escape action (Bound to Escape / Start Button by Default.)
        /// </summary>
        public static void Escape()
        {
            if (PauseMenu.Loaded && !PauseMenu.Active)
                PauseMenu.Get().Open();
            else if (currentMenus.Count > 0)
                currentMenus[^1].Close();
        }
    }
}
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
