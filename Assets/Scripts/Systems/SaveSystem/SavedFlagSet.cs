using AYellowpaper.SerializedCollections;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "SerializedFlagSet", menuName = "ScriptableObjects/SerializedFlagSet")]
public class SavedFlagSet : ScriptableObject, ICloneable<SavedFlagSet>
{
    public SerializedDictionary<string, bool> flags;

    public void LoadFromJson(JToken json)
    {
        foreach (var key in flags.Keys)
        {
            if (json[key] != null) flags[key] = (bool)json[key];
        }
    }

    public void Clone(ref SavedFlagSet target)
    {
        if (target == null) target = Instantiate(this);
        else
        {
            target.flags.Clear();
            foreach (var key in flags.Keys) 
                target.flags.Add(key, flags[key]);
        }
    }
}
