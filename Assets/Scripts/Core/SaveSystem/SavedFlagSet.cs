using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RageRooster.Systems.SaveSystem.Flags
{
    /// <summary>
    /// A collection of <see cref="Flag"/>s that can be saved to disk to track changes in the world. <br/>
    /// One exists for each area and one globally.
    /// </summary>
    [CreateAssetMenu(fileName = "SerializedFlagSet", menuName = "ScriptableObjects/SerializedFlagSet")]
    public class SavedFlagSet : ScriptableObject, ICloneable<SavedFlagSet>
    {
        [SerializeField]
        private Polymorph.ListOf<Flag> _flagList;

        public void InitDictionary()
        {
            flags = new();
            for (int i = 0; i < _flagList.Count; i++) 
                flags[_flagList[i].Name] = _flagList[i];
        }
        public Dictionary<string, Flag> flags;
        

        public void LoadFromJson(JToken json)
        {
            if (flags == null) InitDictionary();
            foreach (var pair in flags)
                pair.Value.LoadFromJson((JValue)json[pair.Key]);
        }

        public JObject SaveToJson()
        {
            if (flags == null) InitDictionary();
            var result = new JObject();

            foreach (var pair in flags) result[pair.Key] = pair.Value.SaveToJson();
            return result;
        }


        public SavedFlagSet Clone(SavedFlagSet target = null)
        {
            if (target == null) target = Instantiate(this);
            else
            {
                foreach (string key in flags.Keys)
                    flags[key].Clone(target.flags[key]);
            }
            return target;
        }


        /// <summary>
        /// Tries to get a flag value from the dictionary.
        /// </summary>
        /// <typeparam name="T">The type assumed to be in this slot.</typeparam>
        /// <param name="key">The name identifier of the flag.</param>
        /// <param name="value">The output value of the flag.</param>
        /// <returns>Whether acquiring the flag was a success.</returns>
        public bool TryGetFlag<T>(string key, out T value)
        {
            if (flags == null) InitDictionary();
            value = default;
            return flags.ContainsKey(key) && flags[key].TryGetValue(out value);
        }

        /// <summary>
        /// Tries to set a flag value in the dictionary.
        /// </summary>
        /// <typeparam name="T">The type assumed to be in this slot.</typeparam>
        /// <param name="key">The name identifier of the flag.</param>
        /// <param name="value">The new value to set for the flag.</param>
        /// <returns>Whether setting the flag was a success.</returns>
        public bool TrySetFlag<T>(string key, T value)
        {
            if (flags == null) InitDictionary();
            return flags.ContainsKey(key) && flags[key].TrySetValue(value);
        }

    }
}