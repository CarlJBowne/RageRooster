using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.Progress;

[System.Serializable] 
public class ZoneManager : Singleton<ZoneManager>
{
    [SerializeField] ZoneRoot currentZone;
    [SerializeField] AYellowpaper.SerializedCollections.SerializedDictionary<string, ZoneProxy> proxies = new();
    public float minLoadTime;
    public Timer.Loop offsetSetTimer = new(15f);
    public float distanceToOriginShift;

    private Transform playerTransform;
    private PlayerStateMachine playerMachine;
    private Vector3Double currentOffset;

    protected override void OnAwake()
    {
        playerTransform = Gameplay.Player.transform;
        playerMachine = Gameplay.Player.GetComponent<PlayerStateMachine>();
    }

    public void Update()
    {
        foreach (ZoneProxy area in proxies.Values) area.Update();
        offsetSetTimer.Tick(UpdateOffset);
    }


    public static void LoadArea(ZoneRoot zone) { if (Active) Get().LoadArea_(zone); }
    private void LoadArea_(ZoneRoot zone)
    {
        if (currentZone == null)
        {
            currentZone = zone;
            proxies.Add(zone, new(zone));
        }
        else proxies[zone.name].root = zone;

        zone.transform.position = zone.originOffset + currentOffset;
    }
    
    public static void DoTransition(string sceneName) { if (Active) Get().DoTransition_(sceneName); }
    private void DoTransition_(string sceneName)
    {
        if (sceneName == currentZone.name) return;
        currentZone = proxies[sceneName].GetRoot();
    }

    public static bool IsCurrent(ZoneProxy zone) => Active && Get().currentZone == zone;

    public static void AddTransition(ZoneTransition transition) { if (Active) Get().AddTransition_(transition); }
    private void AddTransition_(ZoneTransition transition)
    {
        if (!proxies.ContainsKey(transition)) proxies.Add(transition, new(transition));
        else proxies[transition].transitionsTo.Add(transition);
    }
    public static void RemoveTransition(ZoneTransition transition) { if (Active) Get().RemoveTransition_(transition); }
    private void RemoveTransition_(ZoneTransition transition)
    {

        if (!proxies.TryGetValue(transition, out ZoneProxy proxy)) 
            throw new System.Exception("How are you trying to remove a Transition from a zone that has no Proxy loaded?");
        proxy.transitionsTo.Remove(transition);
        if (currentZone != proxy && proxy.transitionsTo.Count == 0)
        {
            if(proxy.loaded) SceneManager.UnloadSceneAsync(proxy.name);
            proxies.Remove(transition);
        }
    }


    private void UpdateOffset()
    {
        if(playerMachine.IsStableForOriginShift() && 
           playerTransform.position.x > distanceToOriginShift || playerTransform.position.x < -distanceToOriginShift ||
           playerTransform.position.y > distanceToOriginShift || playerTransform.position.y < -distanceToOriginShift ||
           playerTransform.position.z > distanceToOriginShift || playerTransform.position.z < -distanceToOriginShift)
        {
            currentOffset -= playerTransform.position;
            playerMachine.InstantMove(Vector3.zero);

            currentZone.transform.position = currentZone.originOffset + currentOffset;
            foreach (ZoneProxy item in proxies.Values)
                if (item.loaded) item.root.transform.position = item.root.originOffset + currentOffset;
        }
    }
}

public struct Vector3Double
{
    public double x;
    public double y;
    public double z;

    public Vector3Double(double x, double y, double z)
    {
        this.x = x; 
        this.y = y; 
        this.z = z;
    }

    public static implicit operator Vector3(Vector3Double v) => new((float)v.x, (float)v.y, (float)v.z);
    public static Vector3Double operator -(Vector3Double This, Vector3 Other)
    {
        This.x -= Other.x;
        This.y -= Other.y;
        This.z -= Other.z;
        return This;
    }
}