using Newtonsoft.Json.Linq;
using SLS.ListUtilities;
using SLS.SaveData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Utilities;

namespace RageRooster.Core.Save
{
    /// <summary>
    /// A collection of <see cref="FlagBase"/>s that can be saved to disk to track changes in the world. <br/>
    /// One exists for each area and one globally.
    /// </summary>
    [CreateAssetMenu(fileName = "SerializedFlagSet", menuName = "ScriptableObjects/SerializedFlagSet"), Serializable]
    public class SavedFlagSet : Saveable<SavedFlagSet>
    {
        public static SavedFlagSet ActiveGlobal { get; private set; }
        public static Dictionary<string, SavedFlagSet> Actives { get; private set; }
        public static void Establish(SavedFlagSet global, Dictionary<string, SavedFlagSet> areaFlags)
        {
            ActiveGlobal = global;
            Actives = areaFlags;
        }

        [SerializeField]
        protected Polymorph.Dictionary<FlagBase> flags;

        public override void Clone(SavedFlagSet source) => flags.Clone(source.flags);

        /// <summary>
        /// Tries to get a flag value from the dictionary.
        /// </summary>
        /// <typeparam name="T">The type assumed to be in this slot.</typeparam>
        /// <param name="name">The name identifier of the flag.</param>
        /// <param name="value">The output value of the flag.</param>
        /// <returns>Whether acquiring the flag was a success.</returns>
        public bool TryGetFlag<T>(string name, out T value)
        {
            value = default;
            return flags.ContainsName(name) && flags[name].TryGetValue(out value);
        }

        /// <summary>
        /// Tries to set a flag value in the dictionary.
        /// </summary>
        /// <typeparam name="T">The type assumed to be in this slot.</typeparam>
        /// <param name="name">The name identifier of the flag.</param>
        /// <param name="value">The new value to set for the flag.</param>
        /// <returns>Whether setting the flag was a success.</returns>
        public bool TrySetFlag<T>(string name, T value) => flags.ContainsName(name) && flags[name].TrySetValue(value);
        /// <summary>
        /// Tries to get a flag value from the dictionary.
        /// </summary>
        /// <typeparam name="T">The type assumed to be in this slot.</typeparam>
        /// <param name="hash">The name identifier of the flag.</param>
        /// <param name="value">The output value of the flag.</param>
        /// <returns>Whether acquiring the flag was a success.</returns>
        public bool TryGetFlag<T>(int hash, out T value)
        {
            value = default;
            return flags.ContainsName(hash) && flags[hash].TryGetValue(out value);
        }

        /// <summary>
        /// Tries to set a flag value in the dictionary.
        /// </summary>
        /// <typeparam name="T">The type assumed to be in this slot.</typeparam>
        /// <param name="hash">The name identifier of the flag.</param>
        /// <param name="value">The new value to set for the flag.</param>
        /// <returns>Whether setting the flag was a success.</returns>
        public bool TrySetFlag<T>(int hash, T value) => flags.ContainsKey(hash) && flags[hash].TrySetValue(value);

        public bool TryLoadFromJson(string key, JToken value)
        {

        }
        public static implicit operator JObject(SavedFlagSet set)
        {
            JObject result = new();

            set.flags.ForEach((name, hash, value) => { result.Add(name, value); });

            return result;
        }

        public class StoryFlags : SavedFlagSet
        {
            public float CompletionOf(float percentage)
            {
                int Count = flags.Count;
                int completeted = 0;
                for (int i = 0; i < flags.Count; i++)
                {
                    if(flags.ValueFromIndex(i) is FlagBase.Flag<bool> flag)
                    { if (flag.value) completeted++; }
                    else Count--;
                }
                return completeted / Count * percentage;
            }
        }
    }
}