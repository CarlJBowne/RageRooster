using Newtonsoft.Json.Linq;
using RageRooster.World;
using System;
using System.Collections.Generic;
using System.IO;
using Utilities.JSON;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

namespace RageRooster.SaveSystem
{
    /// <summary>
    /// A class tracking saved values across the game.
    /// </summary>
    public class SaveData
    {
        /// <summary>
        /// The currently active Save Data during Gameplay.
        /// </summary>
        public static SaveData Current;
        /// <summary>
        /// The Save Data used to reload data after the player experiences a death.    
        /// </summary>
        /// <remarks> See <see cref="RevertToDeathData"/></remarks>
        public static SaveData DeathReloadData;

        #region Actual Data

        public const string targetFileVersion = "1.0.0";
        public DestinationMap location;

        public SavedPlayerStats playerStats = new();
        public class SavedPlayerStats
        {
            /// <summary>
            /// Don't access this directly outside of the SaveFile System. Use <see cref="Player.Health.MaxHealth"/> instead.
            /// </summary>
            public int maxHealth = 3;
            /// <summary>
            /// Don't access this directly outside of the SaveFile System. Use <see cref="Player.Ammo.MaxAmmo"/> instead.
            /// </summary>
            public int maxAmmo = 0;
            /// <summary>
            /// Don't access this directly outside of the SaveFile System. Use <see cref="Player.Currency.Amount"/> instead.
            /// </summary>
            public int currency = 0;
            public TimeSpan playTime = TimeSpan.Zero;
            public ISaveable upgrades;
        }


        public SavedCollectible powerEggs = new();
        public SavedCollectible wishbones = new();
        public SavedCollectible hensRescued = new();
        /// <summary>
        /// A Basic Saved Collectible class, tracking the amount and specific collected instances of a collectible. <br/>
        /// Used for <see cref="powerEggs"/>, <see cref="wishbones"/>, and <see cref="hensRescued"/>.
        /// </summary>
        public class SavedCollectible : ICloneable<SavedCollectible>
        {
            /// <summary>
            /// The total amount of this collectible that has been collected, only for easy access.
            /// </summary>
            public int total = 0;
            /// <summary>
            /// A list of individual collectibles and whether they are collected or not.<br/>
            /// </summary>
            public List<bool> isCollected;

            public SavedCollectible Clone(SavedCollectible target = null)
            {
                target ??= new SavedCollectible();
                target.total = total;
                target.isCollected = new List<bool>(isCollected);
                return target;
            }
        }


        public Flags.SavedFlagSet globalChanges;
        public Dictionary<IAreaAsset, Flags.SavedFlagSet> areaChanges = new();

        #endregion Actual Data 



        /// <summary>
        /// Default Constructor, Clones data from default assets.
        /// </summary>
        /// <remarks>Remarks: For the love of god, if the <see cref="SavedValueRegistry"/> Scriptable Object is missing from the project, we have a problem.</remarks>
        public SaveData()
        {
            location = DestinationMap.Default;
            playerStats.upgrades = SavedValueRegistry.Upgrades.Clone();
            powerEggs.isCollected = new(new bool[SavedValueRegistry.PowerEggs.Count]);
            hensRescued.isCollected = new(new bool[SavedValueRegistry.HensRescued.Count]);
            wishbones.isCollected = new(new bool[SavedValueRegistry.Wishbones.Count]);
            globalChanges = SavedValueRegistry.GlobalFlagDefaults.Clone();
            foreach (var area in DestinationMap.AllAreas)
                areaChanges.Add(area, area.flagDefaults.Clone());
        }

        public static void InitializeSaves(int fileNo)
        {
            IO = new(fileNo);
            Current = new();
            DeathReloadData = new();
            RevertToSaveFile();
        }

        public static void Clone(SaveData source, SaveData target)
        {
            source ??= new SaveData();
            target ??= new SaveData();

            target.location = source.location;

            target.playerStats.maxHealth = source.playerStats.maxHealth;
            target.playerStats.maxAmmo = source.playerStats.maxAmmo;
            target.playerStats.currency = source.playerStats.currency;
            target.playerStats.playTime = source.playerStats.playTime;
            source.playerStats.upgrades.Transfer(target.playerStats.upgrades);

            source.powerEggs.Clone(target.powerEggs);
            source.wishbones.Clone(target.wishbones);
            source.hensRescued.Clone(target.hensRescued);
            source.globalChanges.Clone(target.globalChanges);
            foreach (IAreaAsset area in DestinationMap.AllAreas)
                target.areaChanges[area].CloneFrom(source.areaChanges[area]);
        }

        /// <summary>
        /// The active IO Stream for saving data during gameplay.
        /// </summary>
        public static IOStream IO;

        /// <summary>
        /// An Input Output stream for Saving/Loading Save Data to/from disk. Also used to display save files in UI.
        /// </summary>
        public class IOStream : JsonStream<SaveData>
        {
            public IOStream(int fileID)
            {
                this.fileID = fileID;
                saveRootPath = $"{Application.persistentDataPath}/Save{fileID}";

                RootFile = new(saveRootPath, $"playerData");
                WorldChangesFile = new(saveRootPath, $"worldChanges");
                areaChangesFiles = new();
                foreach (IAreaAsset area in DestinationMap.AllAreas)
                    areaChangesFiles.Add(area, new JsonFile(saveRootPath, $"flags_{area.name}"));
                SecondaryFiles = areaChangesFiles.Values.Append(WorldChangesFile).ToArray();
            }

            int fileID;

            public JsonFile PlayerFile => RootFile;

            //Contains powerEggs, hensRescued, and globalChanges
            public JsonFile WorldChangesFile;

            public Dictionary<IAreaAsset, JsonFile> areaChangesFiles;

            protected override JsonFile.LoadResult ReadData(SaveData ResultingData)
            {
                ResultingData.location = (DestinationMap)PlayerFile.Data[nameof(ResultingData.location)];
                ResultingData.playerStats.maxHealth = (int)PlayerFile.Data[nameof(SavedPlayerStats.maxHealth)];
                ResultingData.playerStats.maxAmmo = (int)PlayerFile.Data[nameof(SavedPlayerStats.maxAmmo)];
                ResultingData.playerStats.currency = (int)PlayerFile.Data[nameof(SavedPlayerStats.currency)];
                ResultingData.playerStats.playTime = TimeSpan.Parse((string)PlayerFile.Data[nameof(SavedPlayerStats.playTime)]);

                JToken upgradesLoad = PlayerFile.Data[nameof(SavedPlayerStats.upgrades)];
                ResultingData.playerStats.upgrades = upgradesLoad.ToObject<Upgrades>();

                JToken powerEggsLoad = WorldChangesFile.Data[nameof(ResultingData.powerEggs)];
                JToken wishbonesLoad = WorldChangesFile.Data[nameof(ResultingData.wishbones)];
                JToken hensRescuedLoad = WorldChangesFile.Data[nameof(ResultingData.hensRescued)];
                JToken globalChangesLoad = WorldChangesFile.Data[nameof(ResultingData.globalChanges)];

                ResultingData.powerEggs.total = (int)powerEggsLoad[nameof(SavedCollectible.total)];
                for (int i = 0; i < ResultingData.powerEggs.isCollected.Count; i++)
                    ResultingData.powerEggs.isCollected[i] = (bool)powerEggsLoad[nameof(SavedCollectible.isCollected)][i];

                ResultingData.wishbones.total = (int)wishbonesLoad[nameof(SavedCollectible.total)];
                for (int i = 0; i < ResultingData.wishbones.isCollected.Count; i++)
                    ResultingData.wishbones.isCollected[i] = (bool)wishbonesLoad[nameof(SavedCollectible.isCollected)][i];

                ResultingData.hensRescued.total = (int)hensRescuedLoad[nameof(SavedCollectible.total)];
                for (int i = 0; i < ResultingData.hensRescued.isCollected.Count; i++)
                    ResultingData.hensRescued.isCollected[i] = (bool)hensRescuedLoad[nameof(SavedCollectible.isCollected)][i];

                ResultingData.globalChanges.LoadFromJson(globalChangesLoad);

                foreach (IAreaAsset area in DestinationMap.AllAreas)
                    ResultingData.areaChanges[area].LoadFromJson(areaChangesFiles[area].Data);

                return JsonFile.LoadResult.Success;
            }
            protected override JsonFile.FileState WriteData(SaveData sourceData)
            {

                PlayerFile.Data = new JObject
                {
                    ["FileVersion"] = targetFileVersion,
                    [nameof(sourceData.location)] = (JToken)sourceData.location,
                    [nameof(SavedPlayerStats.playTime)] = sourceData.playerStats.playTime,
                    [nameof(SavedPlayerStats.maxHealth)] = sourceData.playerStats.maxHealth,
                    [nameof(SavedPlayerStats.maxAmmo)] = sourceData.playerStats.maxAmmo,
                    [nameof(SavedPlayerStats.currency)] = sourceData.playerStats.currency,
                    [nameof(SavedPlayerStats.playTime)] = sourceData.playerStats.playTime.ToString(),
                    [nameof(SavedPlayerStats.upgrades)] = JObject.FromObject(sourceData.playerStats.upgrades)
                };

                WorldChangesFile.Data = new JObject
                {
                    [nameof(sourceData.powerEggs)] = new JObject
                    {
                        [nameof(SavedCollectible.total)] = sourceData.powerEggs.total,
                        [nameof(SavedCollectible.isCollected)] = new JArray(sourceData.powerEggs.isCollected)
                    },
                    [nameof(sourceData.wishbones)] = new JObject
                    {
                        [nameof(SavedCollectible.total)] = sourceData.wishbones.total,
                        [nameof(SavedCollectible.isCollected)] = new JArray(sourceData.wishbones.isCollected)
                    },
                    [nameof(sourceData.hensRescued)] = new JObject
                    {
                        [nameof(SavedCollectible.total)] = sourceData.hensRescued.total,
                        [nameof(SavedCollectible.isCollected)] = new JArray(sourceData.hensRescued.isCollected)
                    },
                    [nameof(sourceData.globalChanges)] = sourceData.globalChanges.SaveToJson()
                };

                // Save areaChanges to areaChangesFiles
                foreach (IAreaAsset area in DestinationMap.AllAreas)
                    areaChangesFiles[area].Data = sourceData.areaChanges[area].SaveToJson();

                // Save all files
                JsonFile.FileState state = PlayerFile.SaveToFile();
                if (state != JsonFile.FileState.Valid) return state;
                state = WorldChangesFile.SaveToFile();
                if (state != JsonFile.FileState.Valid) return state;
                foreach (var pair in areaChangesFiles)
                {
                    state = pair.Value.SaveToFile();
                    if (state != JsonFile.FileState.Valid) return state;
                }

                return JsonFile.FileState.Valid;
            }

            public float GetCompletionPercentage()
            {
                if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");
                int totalCollectibles = SavedValueRegistry.PowerEggs.Count + SavedValueRegistry.Wishbones.Count + SavedValueRegistry.HensRescued.Count;
                if (totalCollectibles == 0) return 100f;
                int collected = 0;
                collected += WorldChangesFile[nameof(powerEggs)][nameof(SavedCollectible.total)].ToObject<int>();
                collected += WorldChangesFile[nameof(wishbones)][nameof(SavedCollectible.total)].ToObject<int>();
                collected += WorldChangesFile[nameof(hensRescued)][nameof(SavedCollectible.total)].ToObject<int>();

                return (collected / (float)totalCollectibles) * 100f;
            }

        }


        /// <summary>
        /// Reverts the current save data to its state at the time of the last Death Checkpoint. <br/>
        /// See <see cref="DeathReloadData"/>
        /// </summary>
        public static void RevertToDeathData()
        {
            Clone(DeathReloadData, Current);
            Player.Health.Max = Current.playerStats.maxHealth;
            Player.Health.Current = Player.Health.Max;
            Player.Ammo.Max = Current.playerStats.maxAmmo;
            Player.Ammo.Current = Player.Ammo.Max;
            Player.Currency.Current = Current.playerStats.currency;
        }
        /// <summary>
        /// Reverts the current save data to the data last saved to disk.
        /// </summary>
        /// <remarks>See <see cref="IO"/>.</remarks>
        public static void RevertToSaveFile()
        {
            IO.LoadFromFile(Current);
            Clone(Current, DeathReloadData);
            Player.Health.Max = Current.playerStats.maxHealth;
            Player.Health.Current = Player.Health.Max;
            Player.Ammo.Max = Current.playerStats.maxAmmo;
            Player.Ammo.Current = Player.Ammo.Max;
            Player.Currency.Current = Current.playerStats.currency;
        }
        /// <summary>
        /// Saves the current Data to disk.
        /// </summary>
        /// <param name="destination">The current location of the player, as will be applied to all active SaveData objects.</param>
        public static void SaveFileToDisk(DestinationMap destination)
        {
            Current.location = destination;

            Current.playerStats.playTime += TimeSpan.FromSeconds(Gameplay.UpdateGameTime());

            IO.SaveToFile(Current);
        }

    }
}
