using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneRoot : MonoBehaviour
{
    public ZoneTransition[] transitions;
    public Transform defaultPlayerSpawn;

    [HideInInspector] public new string name;

    public static implicit operator string(ZoneRoot A) => A.name ?? A.gameObject.scene.name;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Boot()
    {
        var attempt = FindFirstObjectByType<ZoneRoot>(FindObjectsInactive.Include);
        if (attempt == null || ZoneManager.Active) return;
        Gameplay.areaToOpen = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(Gameplay.GAMEPLAY_SCENE_NAME);
        return;
    }

    private void Awake()
    {
        if (!ZoneManager.Active) return;

        name = gameObject.scene.name;

        if(transitions.Length == 0) transitions = gameObject.GetComponentsInChildren<ZoneTransition>();

        ZoneManager.LoadArea(this);

    }

    public void Update_()
    {

    }


}

[System.Serializable]
public class ZoneProxy
{
    public string name;
    public bool loaded;
    public ZoneRoot root;

    public List<ZoneTransition> transitionsTo;
    public Coroutine task;
    public AsyncOperation async;

    private readonly ZoneManager manager;

    public static implicit operator string(ZoneProxy A) => A.name;

    public ZoneProxy(ZoneTransition firstTransition)
    {
        name = firstTransition;
        loaded = SceneManager.GetSceneByName(name).isLoaded;
        transitionsTo = new() { firstTransition };
        ZoneManager.Get(out manager);
    }
    public ZoneProxy(ZoneRoot Root)
    {
        name = Root;
        root = Root;
        loaded = true;
        transitionsTo = new();
        ZoneManager.Get(out manager);
        task = Gameplay.Get().StartCoroutine(LockFromUnloading());
    }

    public void Update()
    {
        if (!ZoneManager.IsCurrent(this) || task != null) return;
        bool value = CheckForLoad();

        if (value && !loaded) task = Gameplay.Get().StartCoroutine(Loading());
        else if (!value && loaded) task = Gameplay.Get().StartCoroutine(Unloading());
    }

    public bool CheckForLoad()
    {
        for (int i = 0; i < transitionsTo.Count; i++)
            if (transitionsTo[i].WithinRange())
                return true;
        return false;
    }

    IEnumerator Loading()
    {
        yield return null;

        async = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
        while (async.progress < 1) yield return null;

        loaded = true;
        SetTraversable(false);
        task = null;
    }
    IEnumerator Unloading()
    {
        yield return new WaitForSecondsRealtime(manager.minLoadTime);
        if (CheckForLoad())
        {
            task = null;
            yield break;
        }

        async = SceneManager.UnloadSceneAsync(name);
        while (async.progress < 1) yield return null;

        loaded = false;
        SetTraversable(true);
        root = null;

        task = CheckForLoad() ? Gameplay.Get().StartCoroutine(Loading()) : null;
    }
    IEnumerator LockFromUnloading()
    {
        yield return new WaitForSecondsRealtime(manager.minLoadTime);
        task = null;
    }

    void SetTraversable(bool value)
    { foreach (ZoneTransition transition in transitionsTo) transition.SetTraversable(value); }

    public ZoneRoot GetRoot() => root != null ? root : throw new System.Exception("ERROR: The Player is attempting to transition to a Zone that has not yet Loaded. Ideally, there would be a collider attached to the visual proxy to prevent this from ever happening.");

}
