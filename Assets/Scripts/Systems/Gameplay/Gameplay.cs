using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class Gameplay : Singleton<Gameplay>
{

    public PlayerStateMachine player;
    public Transform cameraTransform;
    public PauseMenu pauseMenu;
    public UIHUDSystem UI;
    public string defaultAreaScene;
    public float minLoadedTime;

    public static bool active = false;
    public static string areaToOpen = null;
    public const string GAMEPLAY_SCENE_NAME = "GameplayScene";
    public static float MIN_LOADED_TIME;

    protected override void OnAwake()
    {
        active = true;
        MIN_LOADED_TIME = minLoadedTime;
        SceneManager.LoadScene(areaToOpen ?? defaultAreaScene, LoadSceneMode.Additive);
    }
    private void Update()
    {
        areaManager.Update();
    }

    public AreaManager areaManager = new();
    public AreaRoot currentRoot;



}
[System.Serializable] 
public class AreaManager
{
    public static AreaManager Get() => Gameplay.Get().areaManager;

    AreaRoot primaryArea;
    Dictionary<string, AreaSurrounding> surroundingAreas;

    public void LoadArea(AreaRoot area)
    {
        if(primaryArea == null) ActivatePrimaryArea(area);
        else surroundingAreas[area.name].root = area;
    }
    
    public void ActivatePrimaryArea(AreaRoot area)
    {
        primaryArea = area;
        Gameplay.Get().currentRoot = primaryArea;
        surroundingAreas = new();
        foreach (AreaTransition item in primaryArea.transitions)
        {
            if (!surroundingAreas.ContainsKey(item)) surroundingAreas.Add(item, new(item));
            else surroundingAreas[item].leadingTransitions.Add(item);
        }
        foreach (AreaSurrounding areaS in surroundingAreas.Values) areaS.CheckForLoad();
    }

    public void EnactTransition(string sceneName)
    {
        string oldArea = primaryArea;
        if (sceneName == primaryArea.name) return;
        foreach (AreaSurrounding item in surroundingAreas.Values) 
            item.TrueUnload(item == surroundingAreas[sceneName]);
        ActivatePrimaryArea(surroundingAreas[sceneName].GetRoot());
    }

    public void Update()
    {
        foreach (AreaSurrounding area in surroundingAreas.Values) area.Update();
    }


}

[System.Serializable]
public class AreaSurrounding
{
    public string name;
    public List<AreaTransition> leadingTransitions;
    public AreaRoot root;
    public Coroutine task;
    public AsyncOperation async;

    bool loaded;

    public static implicit operator string(AreaSurrounding A) => A.name;

    public AreaSurrounding(AreaTransition firstTransition)
    {
        name = firstTransition;
        loaded = SceneManager.GetSceneByName(name).isLoaded;
        leadingTransitions = new() { firstTransition };
        if (loaded)
        {
            root = SceneManager.GetSceneByName(name).GetRootGameObjects()[0].GetComponent<AreaRoot>();
            task = Gameplay.Get().StartCoroutine(LockFromUnloading());
        }
    }

    public void Update()
    {
        if (task != null) return;
        bool value = CheckForLoad();

        if (value && !loaded) task = Gameplay.Get().StartCoroutine(Loading());
        else if (!value && loaded) task = Gameplay.Get().StartCoroutine(Unloading());
    }

    public bool CheckForLoad()
    {
        for (int i = 0; i < leadingTransitions.Count; i++) 
            if (leadingTransitions[i].WithinRange()) 
                return true;
        return false;
    }

    IEnumerator Loading()
    {
        yield return null;

        async = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
        while (async.progress < 1) yield return null;

        loaded = true;
        SetProxies(false);

        yield return new WaitForSecondsRealtime(Gameplay.MIN_LOADED_TIME);
        task = !CheckForLoad() ? Gameplay.Get().StartCoroutine(Unloading()) : null;
    }
    IEnumerator Unloading()
    {
        yield return null;

        async = SceneManager.UnloadSceneAsync(name);
        while (async.progress < 1) yield return null;

        loaded = false;
        SetProxies(true);

        task = CheckForLoad() ? Gameplay.Get().StartCoroutine(Loading()) : null;
    }
    IEnumerator LockFromUnloading()
    {
        yield return new WaitForSecondsRealtime(Gameplay.MIN_LOADED_TIME);
        task = !CheckForLoad() ? Gameplay.Get().StartCoroutine(Unloading()) : null;
    }

    void SetProxies(bool value)
    {
        foreach (AreaTransition transition in leadingTransitions) if (transition.visualProxy != null) transition.visualProxy.SetActive(!value);
    }

    public AreaRoot GetRoot() => root != null ? root : throw new System.Exception("FUCK, THE SCENE WASN'T LOADED YET. FIX PLS");

    public void TrueUnload(bool newMain)
    {
        if (async != null) async.allowSceneActivation = false;
        if (task != null)
        {
            Gameplay.Get().StopCoroutine(task);
            task = null;
        }
        if(!newMain) SceneManager.UnloadSceneAsync(name);
    }
}