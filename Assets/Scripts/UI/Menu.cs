using EditorAttributes;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Menu : MonoBehaviour
{
    //Config
    [DisableInPlayMode] public bool isActive;
    [DisableInPlayMode] public Menu parent;
    [SerializeField] private string putInDictionary;
    [SerializeField] private bool closeOverride;
    [SerializeField, ShowField(nameof(closeOverride))] private UnityEvent closeEvent;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference closeSound;

    //Data
    public bool isCurrent => Manager.currentMenu == this;
    public bool isSubMenu => parent != null;


    protected virtual void Awake()
    {
        if(isActive) Manager.Open(this); 
        else
        {
            gameObject.SetActive(false);
        }

        if (!string.IsNullOrEmpty(putInDictionary)) Manager.menuDictionary.Add(putInDictionary, this);
    }

    protected virtual void OnDestroy()
    {
        if(!string.IsNullOrEmpty(putInDictionary)) Manager.menuDictionary.Remove(putInDictionary);
        isActive = false;
        Manager.Close(this);
    }

    public void Open() => Manager.Open(this);
    public void Close()
    {
        if (!closeOverride) Manager.Close(this);
        else closeEvent?.Invoke();
    }
    public void TrueClose() => Manager.Close(this);


    protected virtual void OnOpen() { if(!openSound.IsNull) AudioManager.Get().PlayOneShot(openSound,transform .position); }
    protected virtual void OnClose() { if (!closeSound.IsNull) AudioManager.Get().PlayOneShot(closeSound, transform.position); }



    public static class Manager
    {
        public static Menu currentMenu => currentMenus[^1];
        public static List<Menu> currentMenus = new();
        public static Dictionary<string, Menu> menuDictionary = new();
        public static bool disableEscape;

        public static void Open(Menu menu)
        {
            if (menu.isActive) return;

            currentMenus.Add(menu); 

            menu.isActive = true;
            menu.gameObject.SetActive(true);
            menu.OnOpen();
        }

        public static void Close(Menu menu)
        {
            if (!menu.isActive) return;

            currentMenus.Remove(menu);

            menu.gameObject.SetActive(false);
            menu.isActive = false;
            menu.OnClose();
        }

        public static void Escape()
        {
            if (PauseMenu.Loaded && !PauseMenu.Active) PauseMenu.Get().Open();
            else if (currentMenus.Count > 0) currentMenus[^1].Close(); 
        }

    }
}

public abstract class MenuSingleton<T> : Menu where T : MenuSingleton<T>
{
    private static T _instance;
    public static bool Loaded;

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
        if (output == null) throw new Exception("Singleton not active.");
    }

    public static bool Active => Get().isActive;

    protected static T InitFind()
    {
        #if UNITY_EDITOR
        if (!Application.isPlaying) { Debug.LogError($"{typeof(T)} accessed outside of runtime. Don't."); return null; }
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


    protected override void Awake()
    {
        base.Awake();
        if (Loaded) return;
        if (_instance || _instance == this) return;
        _instance = this as T;
        Loaded = true;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Loaded = false;
        _instance = null;
    }






}