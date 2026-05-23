using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Utilities;

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
        private FlagDictionary flags = new();

        public void LoadFromJson(JToken json)
        {
            foreach (var pair in flags)
                pair.Value.LoadFromJson((JValue)json[pair.Key]);
        }

        public JObject SaveToJson()
        {
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
        public bool TrySetFlag<T>(string key, T value) => flags.ContainsKey(key) && flags[key].TrySetValue(value);


        [System.Serializable]
        public class FlagDictionary : SerializedReferenceDictionary<string, Flag>
        {
        }
    }
}