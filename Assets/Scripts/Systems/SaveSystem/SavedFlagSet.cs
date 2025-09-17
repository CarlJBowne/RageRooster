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

    public SavedFlagSet Clone(SavedFlagSet result = null)
    {
        if (result == null) result = Instantiate(this);
        else foreach (var key in result.flags.Keys)
            {
                result.flags[key] = flags.ContainsKey(key)
                    ? flags[key]
                    : false;
            }
        return result;
    }
}
