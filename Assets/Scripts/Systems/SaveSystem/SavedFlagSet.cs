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
        foreach (string key in new List<string>(flags.Keys)) 
            if (json[key] != null) 
                flags[key] = (bool)json[key];
    }

    public SavedFlagSet Clone(SavedFlagSet target = null)
    {
        if (target == null) target = Instantiate(this);
        else
        {
            foreach (string key in new List<string>(target.flags.Keys))
                target.flags[key] = flags[key];
        }
        return target;
    }
}
