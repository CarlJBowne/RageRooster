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

namespace RageRooster.SaveSystem
{
    /// <summary>
    /// A class tracking saved values across the game.
    /// </summary>
    public partial class SaveData : Saveable<SaveData>
    {
        #region Top Systems

        public static void InitializeSystem(SaveData defaultInput)
        {
            defaultInput.Establish(EstablishmentContexts.Default);
            Active = new();
            Active.Establish(EstablishmentContexts.Active);
            //Initialize Actives
            {
                PlayerStats.Active = Active.playerStats;
                SavedProgress.Active = Active.progress;
                SavedCollectible.Hens = Active.progress.hensRescued;
                SavedCollectible.Wishbones = Active.progress.wishbones;
                SavedCollectible.PowerEggs = Active.progress.powerEggs;
            }

            DeathReloadData = new();
            IO.LoadOperator = new();

            //Initialize Save System Services.
            {
                Services.SaveSystem.CurrentDestination = new(
                () => Active.playerStats.location,
                input => Active.playerStats.location = input
            );
                Services.SaveSystem.DeathDestination = new(
                    () => DeathReloadData.playerStats.location,
                    input => DeathReloadData.playerStats.location = input
                );
                Services.SaveSystem.SaveToDeathData = SaveToDeathData;
                Services.SaveSystem.RevertToDeathData = RevertToDeathData;
                Services.SaveSystem.SaveToSaveFile = SaveToSaveFile;
                Services.SaveSystem.RevertToSaveFile = RevertToSaveFile;
            }
        }

        public override void Establish(string context)
        {
            if(context == EstablishmentContexts.Active) Active = this;
            else if(context == EstablishmentContexts.Default) Default = this;

            playerStats.Establish(context);
            progress.Establish(context);
            globalChanges.Establish(context);
        }

        public static class EstablishmentContexts
        {
            public const string Default = "Default";
            public const string Active = "Active";  
        }

        /// <summary> The Save Data used to reload data after the player experiences a death. </summary>
        /// <remarks> See <see cref="RevertToDeathData"/></remarks>
        public static SaveData DeathReloadData { get; private set; }


        public static void SaveToDeathData() => Clone(Active, DeathReloadData);
        public static void RevertToDeathData() => Clone(DeathReloadData, Active);

        public static void SaveToSaveFile()
        {
            Active.progress.playTime += TimeSpan.FromSeconds(SavedProgress.UpdateGameTime());
            Clone(Active, DeathReloadData);
            Clone(Active, IO.LoadOperator);
            IO.SaveToFile();
        }
        public static void RevertToSaveFile()
        {
            SavedProgress.UpdateGameTime();
            IO.LoadFromFile();
            Clone(IO.LoadOperator, Active);
            Clone(Active, DeathReloadData);
        }

        public static void InitializeSaves(int fileNo)
        {
            IO = new(fileNo);
            Active = new();
            DeathReloadData = new();
            RevertToSaveFile();
        }

        #endregion

        #region Actual Data

        public const string targetFileVersion = "1.0.0";

        public PlayerStats playerStats = new();
        public SavedProgress progress = new();


        public Flags.SavedFlagSet globalChanges;
        public Dictionary<string, Flags.SavedFlagSet> areaChanges = new();

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
            if (areaChanges.Count == 0) areaChanges = new Dictionary<string, Flags.SavedFlagSet>(source.areaChanges);
            foreach (KeyValuePair<string, Flags.SavedFlagSet> pair in areaChanges)
                pair.Value.Clone(source.areaChanges[pair.Key]);
        }

        #endregion

        #region File Stream

        /// <summary> The active IO Stream for saving data during gameplay. </summary>
        public static IOStream IO;

        /// <summary>
        /// An Input Output stream for Saving/Loading Save Data to/from disk. Also used to display save files in UI.
        /// </summary>
        public class IOStream : JsonStream
        {
            public IOStream(int fileID)
            {
                this.fileID = fileID;
                saveRootPath = $"{Application.persistentDataPath}/Save{fileID}";

                RootFile = new(saveRootPath, $"playerData");
                WorldChangesFile = new(saveRootPath, $"worldChanges");
                areaChangesFiles = new();
                foreach (string area in IDestination.AllAreas)
                    areaChangesFiles.Add(area, new JsonFile(saveRootPath, $"flags_{area}"));
                SecondaryFiles = areaChangesFiles.Values.Append(WorldChangesFile).ToArray();
            }

            int fileID;

            public JsonFile PlayerFile => RootFile;

            //Contains powerEggs, hensRescued, and globalChanges
            public JsonFile WorldChangesFile;

            public Dictionary<string, JsonFile> areaChangesFiles;

            protected override JsonFile.LoadResult ReadData()
            {
                //ResultingData.location = (DestinationMap)PlayerFile.Data[nameof(ResultingData.location)];
                //ResultingData.playerStats.maxHealth = (int)PlayerFile.Data[nameof(SavedPlayerStats.maxHealth)];
                //ResultingData.playerStats.maxAmmo = (int)PlayerFile.Data[nameof(SavedPlayerStats.maxAmmo)];
                //ResultingData.playerStats.currency = (int)PlayerFile.Data[nameof(SavedPlayerStats.currency)];
                //ResultingData.playerStats.playTime = TimeSpan.Parse((string)PlayerFile.Data[nameof(SavedPlayerStats.playTime)]);
                //
                //JToken upgradesLoad = PlayerFile.Data[nameof(SavedPlayerStats.upgrades)];
                //ResultingData.playerStats.upgrades = upgradesLoad.ToObject<Upgrades>();
                //
                //JToken powerEggsLoad = WorldChangesFile.Data[nameof(ResultingData.powerEggs)];
                //JToken wishbonesLoad = WorldChangesFile.Data[nameof(ResultingData.wishbones)];
                //JToken hensRescuedLoad = WorldChangesFile.Data[nameof(ResultingData.hensRescued)];
                //JToken globalChangesLoad = WorldChangesFile.Data[nameof(ResultingData.globalChanges)];
                //
                //ResultingData.powerEggs.total = (int)powerEggsLoad[nameof(SavedCollectible.total)];
                //for (int i = 0; i < ResultingData.powerEggs.isCollected.Count; i++)
                //    ResultingData.powerEggs.isCollected[i] = (bool)powerEggsLoad[nameof(SavedCollectible.isCollected)][i];
                //
                //ResultingData.wishbones.total = (int)wishbonesLoad[nameof(SavedCollectible.total)];
                //for (int i = 0; i < ResultingData.wishbones.isCollected.Count; i++)
                //    ResultingData.wishbones.isCollected[i] = (bool)wishbonesLoad[nameof(SavedCollectible.isCollected)][i];
                //
                //ResultingData.hensRescued.total = (int)hensRescuedLoad[nameof(SavedCollectible.total)];
                //for (int i = 0; i < ResultingData.hensRescued.isCollected.Count; i++)
                //    ResultingData.hensRescued.isCollected[i] = (bool)hensRescuedLoad[nameof(SavedCollectible.isCollected)][i];
                //
                //ResultingData.globalChanges.LoadFromJson(globalChangesLoad);
                //
                //foreach (IAreaAsset area in DestinationMap.AllAreas)
                //    ResultingData.areaChanges[area].LoadFromJson(areaChangesFiles[area].Data);
                //
                return JsonFile.LoadResult.Success;
            }
            protected override JsonFile.FileState WriteData()
            {

                //PlayerFile.Data = new JObject
                //{
                //    ["FileVersion"] = targetFileVersion,
                //    [nameof(sourceData.location)] = (JToken)sourceData.location,
                //    [nameof(SavedPlayerStats.playTime)] = sourceData.playerStats.playTime,
                //    [nameof(SavedPlayerStats.maxHealth)] = sourceData.playerStats.maxHealth,
                //    [nameof(SavedPlayerStats.maxAmmo)] = sourceData.playerStats.maxAmmo,
                //    [nameof(SavedPlayerStats.currency)] = sourceData.playerStats.currency,
                //    [nameof(SavedPlayerStats.playTime)] = sourceData.playerStats.playTime.ToString(),
                //    [nameof(SavedPlayerStats.upgrades)] = JObject.FromObject(sourceData.playerStats.upgrades)
                //};
                //
                //WorldChangesFile.Data = new JObject
                //{
                //    [nameof(sourceData.powerEggs)] = new JObject
                //    {
                //        [nameof(SavedCollectible.total)] = sourceData.powerEggs.total,
                //        [nameof(SavedCollectible.isCollected)] = new JArray(sourceData.powerEggs.isCollected)
                //    },
                //    [nameof(sourceData.wishbones)] = new JObject
                //    {
                //        [nameof(SavedCollectible.total)] = sourceData.wishbones.total,
                //        [nameof(SavedCollectible.isCollected)] = new JArray(sourceData.wishbones.isCollected)
                //    },
                //    [nameof(sourceData.hensRescued)] = new JObject
                //    {
                //        [nameof(SavedCollectible.total)] = sourceData.hensRescued.total,
                //        [nameof(SavedCollectible.isCollected)] = new JArray(sourceData.hensRescued.isCollected)
                //    },
                //    [nameof(sourceData.globalChanges)] = sourceData.globalChanges.SaveToJson()
                //};
                //
                //// Save areaChanges to areaChangesFiles
                //foreach (IAreaAsset area in DestinationMap.AllAreas)
                //    areaChangesFiles[area].Data = sourceData.areaChanges[area].SaveToJson();
                //
                //// Save all files
                //JsonFile.FileState state = PlayerFile.SaveToFile();
                //if (state != JsonFile.FileState.Valid) return state;
                //state = WorldChangesFile.SaveToFile();
                //if (state != JsonFile.FileState.Valid) return state;
                //foreach (var pair in areaChangesFiles)
                //{
                //    state = pair.Value.SaveToFile();
                //    if (state != JsonFile.FileState.Valid) return state;
                //}

                return JsonFile.FileState.Valid;
            }

            public float GetCompletionPercentage()
            {
                if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");
                int totalCollectibles = 0 // SavedValueRegistry.PowerEggs.Count + SavedValueRegistry.Wishbones.Count + SavedValueRegistry.HensRescued.Count
                                          ;
                if (totalCollectibles == 0) return 100f;
                int collected = 0;
                //collected += WorldChangesFile[nameof(powerEggs)][nameof(SavedCollectible.total)].ToObject<int>();
                //collected += WorldChangesFile[nameof(wishbones)][nameof(SavedCollectible.total)].ToObject<int>();
                //collected += WorldChangesFile[nameof(hensRescued)][nameof(SavedCollectible.total)].ToObject<int>();

                return (collected / (float)totalCollectibles) * 100f;
            }

            public SaveData LoadOperator;
        }


        #endregion

    }
}
