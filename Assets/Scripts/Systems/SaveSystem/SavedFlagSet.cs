using AYellowpaper.SerializedCollections;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SerializedFlagSet", menuName = "ScriptableObjects/SerializedFlagSet")]
public class SavedFlagSet : ScriptableObject
{
    public SerializedDictionary<string, bool> flags;

    public void LoadFromJson(JToken json)
    {
        foreach (var key in flags.Keys)
        {
            if (json[key] != null) flags[key] = (bool)json[key];
        }
    }
}
