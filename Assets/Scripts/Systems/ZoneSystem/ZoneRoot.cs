using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
public class ZoneRoot : MonoBehaviour
{
    public ZoneTransition[] transitions;
    public SavePoint defaultPlayerSpawn;
    public EventReference music;

    public SavePoint[] spawns {get{
            if (_spawns == null || _spawns.Length == 0) 
                _spawns = gameObject.GetComponentsInChildren<SavePoint>();
            return _spawns; 
        }}
    private SavePoint[] _spawns; 
    [HideInInspector] public new string name;
    public Vector3 originOffset;
    private int loadID = -2;
    private Light[] directionalLights;

    public static implicit operator string(ZoneRoot A) => A.name ?? A.gameObject.scene.name;

    private void Awake()
    {
        if (!ZoneManager.Active)
        {
            if (loadID == -2) Gameplay.BeginScene(SceneManager.GetActiveScene().name);
            else Gameplay.BeginSavePoint(SceneManager.GetActiveScene().name, loadID);
            return;
        }

        name = gameObject.scene.name;
        originOffset = transform.position;

        if(transitions == null || transitions.Length == 0) transitions = gameObject.GetComponentsInChildren<ZoneTransition>();
        if(spawns == null || spawns.Length == 0) _spawns = gameObject.GetComponentsInChildren<SavePoint>();
        
        directionalLights = gameObject.GetComponentsInChildren<Light>().Where(l => l.type == LightType.Directional).ToArray();
        foreach (var item in directionalLights) item.enabled = false;

        ZoneManager.LoadZone(this);

    }

    public void Update_()
    {

    }

    public SavePoint GetSpawn(int id) => id == -1 ? defaultPlayerSpawn : spawns[id];

    public void SetSpawn(SavePoint This) => loadID = This.GetID();

    
    public void OnTransition()
    {
        Gameplay.musicEmitter.EventReference = music;
        Gameplay.musicEmitter.Play();
    }

}

[System.Serializable]
public class ZoneProxy
{
    public string name;
    public bool loaded;
    public ZoneRoot root;

    public List<ZoneTransition> transitionsTo;
    public CoroutinePlus task;
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
        task = new (LockFromUnloading(), Gameplay.Get());
    }

    public void Update()
    {
        if (ZoneManager.IsCurrent(this) || task) return;
        bool value = CheckForLoad();

        if (value && !loaded)
        {
            task = new (Loading(), Gameplay.Get());
        }    
        else if (!value && loaded)
        {
            task = new (Unloading(), Gameplay.Get());
        }
            
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
        if (ZoneManager.IsCurrent(this) || CheckForLoad())
        {
            task = null;
            yield break;
        }

        if(ZoneManager.IsSceneLoaded(name)) async = SceneManager.UnloadSceneAsync(name);
        else yield break;  
        while (async.progress < 1) yield return null;

        loaded = false;
        SetTraversable(true);
        root = null;

        task = CheckForLoad() ? new(Loading(), Gameplay.Get()) : null;
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