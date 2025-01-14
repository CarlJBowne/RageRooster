using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
public class ZoneRoot : MonoBehaviour
{
    public ZoneTransition[] transitions;
    public SavePoint[] _spawns;
    public SavePoint defaultPlayerSpawn;

    public SavePoint[] spawns {get{
            if (_spawns == null || _spawns.Length == 0) 
                _spawns = gameObject.GetComponentsInChildren<SavePoint>();
            return _spawns; 
        }}
    [HideInInspector] public new string name;
    public Vector3 originOffset;
    [HideInInspector] public int loadID = -2;

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

        ZoneManager.LoadArea(this);

    }

    public void Update_()
    {

    }

    public SavePoint GetSpawn(int id) => id == -1 ? defaultPlayerSpawn : spawns[id];
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
        if (ZoneManager.IsCurrent(this) || task != null) return;
        bool value = CheckForLoad();

        if (value && !loaded)
        {
            task = Gameplay.Get().StartCoroutine(Loading());
        }    
        else if (!value && loaded)
        {
            task = Gameplay.Get().StartCoroutine(Unloading());
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

//public struct SpawnPoint
//{
//    public Vector3 position; 
//    public float yRotation;
//
//    public SpawnPoint(Vector3 position, float yRotation)
//    {
//        this.position = position;
//        this.yRotation = yRotation;
//    }
//
//    public static implicit operator SpawnPoint(Transform original) => new(original.position, original.eulerAngles.y);
//}