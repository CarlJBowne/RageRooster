using AYellowpaper.SerializedCollections;
using System;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace RageRooster.Systems.SaveSystem.Variables
{
    [CreateAssetMenu(fileName = "newFile", menuName = "Save System/Saved Variable Set")]
    public class SavedVariableSet : ScriptableObject
    {
        public SerializedDictionary<string, Variable> flags;
    }
}
