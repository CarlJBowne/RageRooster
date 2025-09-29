using AYellowpaper.SerializedCollections;
using System;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace RageRooster.Systems.SaveSystem.Variables
{
    public class SavedVariableSet : ScriptableObject
    {
        Variable var1;
        public SerializedDictionary<string, Variable> flags;
    }
}
