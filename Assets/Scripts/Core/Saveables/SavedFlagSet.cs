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
        private Polymorph.Dictionary<FlagBase> flags;

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
    }
}