using System;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using SLS.ListUtilities;

namespace RageRooster.SaveSystem.Variables
{
    public class SavedVariableSet : ScriptableObject
    {
        public DictionaryS<string, Variable> flags;
    }
}
