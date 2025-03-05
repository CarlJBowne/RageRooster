using AYellowpaper.SerializedCollections;
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

        LoadEvents();

        ZoneManager.LoadZone(this);

    }

    private void OnDestroy() => UnloadEvents();

    public void Update_()
    {

    }

    public SavePoint GetSpawn(int id) => id == -1 ? defaultPlayerSpawn : spawns[id];

    public void SetSpawn(SavePoint This) => loadID = This.GetID();

    
    public void OnTransition()
    {
        Gameplay.musicEmitter.CrossFadeMusic(music);
    }

    public SerializedDictionary<WorldChange, UltEvents.UltEvent> worldChangeEvents;

    private void LoadEvents()
    {
        foreach (KeyValuePair<WorldChange, UltEvents.UltEvent> item in worldChangeEvents)
            if (item.Key.Enabled) item.Value?.Invoke();
            else item.Key.Action += item.Value.InvokeSafe;
    }
    private void UnloadEvents()
    {
        foreach (KeyValuePair<WorldChange, UltEvents.UltEvent> item in worldChangeEvents)
            item.Key.Action -= item.Value.InvokeSafe;
    }

}