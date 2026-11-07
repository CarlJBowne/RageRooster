using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using RageRooster.Core;
using RageRooster.Player;
using RageRooster.Core.World;
using UnityEngine;
using SLS.SaveFileCore;

namespace RageRooster.Core.Save
{
    [Serializable]
    /// <summary>
    /// A class tracking saved values across the game.
    /// </summary>
    public class SaveData : Saveable<SaveData>
    {
        #region Top Systems

        /// <summary>
        /// The currently Active <see cref="SaveData"/> functioning in the game.
        /// </summary>
        public static SaveData Active => SaveDataManager.ActiveData;
        /// <summary>
        /// Stands for Defaults or Definitions depending on your point of view. <br/>
        /// Contains all the default values as well as the definition of what they are called.
        /// </summary>
        public static SaveData Defs => SaveDataManager.DefsData;
        /// <summary> The Save Data used to reload data after the player experiences a death. </summary>
        /// <remarks> See <see cref="RevertToDeathData"/></remarks>
        public static SaveData DeathReloadData => SaveDataManager.DeathReloadData;

        public static void InitializeFile(int fileNo) => SaveDataManager.Get.InitializeFile(fileNo);
        public static void ExitFile() => SaveDataManager.Get.ExitFile();



        public static void SaveToDeathData() => SaveDataManager.Get.SaveToDeathData();
        public static void RevertToDeathData() => SaveDataManager.Get.RevertToDeathData();
        public static void SaveToFile() => SaveDataManager.Get.SaveToFile();
        public static void RevertToFile() => SaveDataManager.Get.RevertToFile();


        public static IReadOnlyList<string> FlagIDs => Defs.flags.Keys.ToList();

        #endregion

        public PlayerStats playerStats = new();
        public SavedProgress progress = new();
        public Dictionary<string, Flag.Collection> flags = new();
        public float Completion =>
            progress.storyFlags.CompletionOf(.4f) +
            progress.powerEggs.CompletionOf(.3f) +
            progress.hensRescued.CompletionOf(.2f) +
            progress.wishbones.CompletionOf(.1f);

        /// <summary>
        /// Default Constructor, Clones data from default assets.
        /// </summary>
        /// <remarks>Remarks: For the love of god, if the <see cref="SavedValueRegistry"/> Scriptable Object is missing from the project, we have a problem.</remarks>
        public SaveData(SaveData source = null)
        {
            if (source != null) Clone(source);
            else if (Defs != null) Clone(Defs);
        }

        public override SaveData Clone(SaveData source)
        {
            playerStats.Clone(source.playerStats);
            progress.Clone(source.progress);
            foreach (string key in source.flags.Keys)
            {
                if (!flags.ContainsKey(key) || flags[key] == source.flags[key])
                    flags[key] = new();
                flags[key].Clone(source.flags[key]);
            }
            return this;
        }

        /// <summary>
        /// The display data used for the Main Menu's Save File Selection Screen. This is a simplified version of the Save Data, containing only the information needed for display purposes.
        /// </summary>
        public class MenuDisplayData
        {
            public bool isValid = true;
            public Destination location;
            public string timeString;
            public float completionPercentage;
            public int health;
            public int powerEggs;
            public int hensRescued;
        }

    }
}
