using Newtonsoft.Json.Linq;
using SLS.ListUtilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Utilities;

namespace RageRooster.SaveSystem.Flags
{
    /// <summary>
    /// A collection of <see cref="Flag"/>s that can be saved to disk to track changes in the world. <br/>
    /// One exists for each area and one globally.
    /// </summary>
    [CreateAssetMenu(fileName = "SerializedFlagSet", menuName = "ScriptableObjects/SerializedFlagSet")]
    public class SavedFlagSet : ScriptableObject, ICloneable<SavedFlagSet>
    {
        [SerializeField]
        private HashedListSReference<Flag> flags = new();

        public void LoadFromJson(JToken json)
        {
            for (int i = 0; i < flags.Count; i++)
            {
                flags.ValueFromIndex(i).LoadFromJson(json[flags.NameFromIndex(i)]);
            }
        }

        public JObject SaveToJson()
        {
            var result = new JObject();

            for (int i = 0; i < flags.Count; i++)
                result[flags.NameFromIndex(i)] = flags.ValueFromIndex(i).SaveToJson();
            return result;
        }


        public SavedFlagSet Clone(SavedFlagSet target = null)
        {
            if (target == null) target = Instantiate(this);
            else
            {
                for (int i = 0; i < flags.Count; i++)
                    flags.ValueFromIndex(i).Clone(target.flags.ValueFromIndex(i));
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
            return flags.ContainsKey(key.Hash()) && flags[key].TryGetValue(out value);
        }

        /// <summary>
        /// Tries to set a flag value in the dictionary.
        /// </summary>
        /// <typeparam name="T">The type assumed to be in this slot.</typeparam>
        /// <param name="key">The name identifier of the flag.</param>
        /// <param name="value">The new value to set for the flag.</param>
        /// <returns>Whether setting the flag was a success.</returns>
        public bool TrySetFlag<T>(string key, T value) => flags.ContainsKey(key.Hash()) && flags[key].TrySetValue(value);
    }
}