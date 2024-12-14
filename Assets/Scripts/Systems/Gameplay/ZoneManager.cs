using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable] 
public class ZoneManager : Singleton<ZoneManager>
{
    [SerializeField] ZoneRoot currentZone;
    [SerializeField] AYellowpaper.SerializedCollections.SerializedDictionary<string, ZoneProxy> proxies = new();
    public float minLoadTime;



    protected override void OnAwake()
    {

    }

    public void Update()
    {
        foreach (ZoneProxy area in proxies.Values) area.Update();
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

}