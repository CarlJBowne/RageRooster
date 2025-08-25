using AYellowpaper.SerializedCollections;
using EditorAttributes;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
public class ZoneRoot : MonoBehaviour
{
    public ZoneTransition[] transitions;
    public SavePoint_Old defaultPlayerSpawn;
    public EventReference music;

    public SavePoint_Old[] spawns {get{
            if (_spawns == null || _spawns.Length == 0) 
                _spawns = gameObject.GetComponentsInChildren<SavePoint_Old>();
            return _spawns; 
        }}
    [SerializeField] private SavePoint_Old[] _spawns; 
    [HideInInspector] public new string name;
    public Vector3 originOffset;
    [SerializeField] private Light[] directionalLights;

    public static implicit operator string(ZoneRoot A) => A.name ?? A.gameObject.scene.name;

    private void Awake()
    {
        if (!ZoneManager.Active)
        {
            if (EditorState.LoadFromSavePointID == -2) Gameplay.BeginScene(SceneManager.GetActiveScene().name);
            else
            {
                Gameplay.BeginSavePoint(SceneManager.GetActiveScene().name, EditorState.LoadFromSavePointID);
                EditorState.LoadFromSavePointID = -2;
            }
            return;
        }

        name = gameObject.scene.name;
        originOffset = transform.position;

        if(transitions == null || transitions.Length == 0) transitions = gameObject.GetComponentsInChildren<ZoneTransition>();
        if(spawns == null || spawns.Length == 0) _spawns = gameObject.GetComponentsInChildren<SavePoint_Old>();
        
        if(directionalLights == null || directionalLights.Length == 0) directionalLights = gameObject.GetComponentsInChildren<Light>().Where(l => l.type == LightType.Directional).ToArray();
        foreach (var item in directionalLights) item.enabled = false;

        LoadEvents();

        ZoneManager.LoadZone(this);

    }

    private void OnDestroy() => UnloadEvents();

    public void Update_()
    {

    }

    public SavePoint_Old GetSpawn(int id) => id == -1 ? defaultPlayerSpawn : spawns[id];

    
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

    [Button]
    public void SetupComponents()
    {
        if (transitions == null || transitions.Length == 0) transitions = gameObject.GetComponentsInChildren<ZoneTransition>();
        if (spawns == null || spawns.Length == 0) _spawns = gameObject.GetComponentsInChildren<SavePoint_Old>();
        directionalLights = gameObject.GetComponentsInChildren<Light>().Where(l => l.type == LightType.Directional).ToArray();
        //foreach (var item in directionalLights) DestroyImmediate(item.gameObject);
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(gameObject);
#endif

    }

}