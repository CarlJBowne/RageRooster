using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using RageRooster.Core;
using RageRooster.Player;
using RageRooster.World;
using SLS.SaveData;
using Unity.VisualScripting;
using UnityEngine;
using Utilities.JSON;

namespace RageRooster.Core.Save
{
    /// <summary>
    /// A class tracking saved values across the game.
    /// </summary>
    public class SaveData : Saveable<SaveData>
    {
        #region Top Systems

        public static SaveData Default { get; private set; }
        public static SaveData Active { get; private set; }

        public static void InitializeDefaults(SaveData defaultInput) => Default = defaultInput;
        public static void InitializeSystem()
        {
            Active = new();
            Active.playerStats.Establish();
            Active.progress.Establish();
            SavedFlagSet.Establish(Active.globalChanges, Active.areaChanges);

            DeathReloadData = new();
        }

        /// <summary> The Save Data used to reload data after the player experiences a death. </summary>
        /// <remarks> See <see cref="RevertToDeathData"/></remarks>
        public static SaveData DeathReloadData { get; private set; }


        public static void SaveToDeathData() => Clone(Active, DeathReloadData);
        public static void RevertToDeathData() => Clone(DeathReloadData, Active);

        public static Action SaveToSaveFile;
        public static Action RevertToSaveFile;
        public static Action<int> CallInitializeSave;

        #endregion

        #region Actual Data

        public PlayerStats playerStats = new();
        public SavedProgress progress = new();
        public SavedFlagSet globalChanges;
        public Dictionary<string, SavedFlagSet> areaChanges = new();
        public float Completion =>
            progress.storyFlags.CompletionOf(.4f) +
            progress.powerEggs.CompletionOf(.3f) + 
            progress.hensRescued.CompletionOf(.2f) + 
            progress.wishbones.CompletionOf(.1f);

        #endregion Actual Data 

        #region Self Functionality

        /// <summary>
        /// Default Constructor, Clones data from default assets.
        /// </summary>
        /// <remarks>Remarks: For the love of god, if the <see cref="SavedValueRegistry"/> Scriptable Object is missing from the project, we have a problem.</remarks>
        public SaveData(SaveData source = null)
        {
            if (source != null) Clone(source);
            else if (Default != null) Clone(Default);
        }

        public override void Clone(SaveData source)
        {
            playerStats.Clone(source.playerStats);
            progress.Clone(source.progress);
            globalChanges.Clone(source.globalChanges);
            if (areaChanges.Count == 0) areaChanges = new(source.areaChanges);
            foreach (KeyValuePair<string, SavedFlagSet> pair in areaChanges)
                pair.Value.Clone(source.areaChanges[pair.Key]);
        }

        #endregion

        #region Editor Exclusive
        public static ScriptableObject SavedValueManagerAsset;

        #endregion

        #region Menu Display Data

        /// <summary>
        /// The display data used for the Main Menu's Save File Selection Screen. This is a simplified version of the Save Data, containing only the information needed for display purposes.
        /// </summary>
        public class MenuDisplayData
        {
            public bool isValid = true;
            public string locationString;
            public string timeString;
            public float completionPercentage;
            public int health;
            public int powerEggs;
            public int hensRescued;
        }

        #endregion
    }
}
