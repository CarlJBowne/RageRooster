using Newtonsoft.Json.Linq;
using RageRooster.RoomSystem;
using System;
using System.Collections.Generic;
using System.IO;

namespace RageRooster.Systems.SaveSystem
{
    public class SaveFile : ICloneable<SaveFile>
    {
        public const string targetFileVersion = "1.0.0";
        public Destination location;
        public SavedPlayerStats playerStats = new();
        public SavedCollectible powerEggs = new();
        public SavedCollectible wishbones = new();
        public SavedCollectible hensRescued = new();
        public SavedFlagSet globalChanges;
        public Dictionary<AreaAsset, SavedFlagSet> areaChanges = new();

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
            public Upgrades upgrades;
        }

        public class SavedCollectible : ICloneable<SavedCollectible>
        {
            public int total = 0;
            public List<bool> isCollected;

            public SavedCollectible Clone(SavedCollectible result = null)
            {
                result ??= new SavedCollectible();
                result.total = total;
                result.isCollected = new List<bool>(isCollected);
                return result;
            }
        }

        /// <summary>
        /// Default Constructor, Clones data from default files.
        /// </summary>
        public SaveFile()
        {
            location = Destination.StartingDefault();
            playerStats.upgrades = SavedValueManager.Upgrades.Clone();
            powerEggs.isCollected = new(new bool[SavedValueManager.PowerEggs.Count]);
            hensRescued.isCollected = new(new bool[SavedValueManager.HensRescued.Count]);
            wishbones.isCollected = new(new bool[SavedValueManager.Wishbones.Count]);
            globalChanges.CloneFrom(SavedValueManager.GlobalFlagDefaults);
            foreach (var area in AreaRegistry.GetAll())
                areaChanges.Add(area, area.flagDefaults.Clone());
        }

        public SaveFile Clone(SaveFile result = null)
        {
            result ??= new SaveFile();

            result.location = location;

            result.playerStats.maxHealth = playerStats.maxHealth;
            result.playerStats.maxAmmo = playerStats.maxAmmo;
            result.playerStats.currency = playerStats.currency;
            result.playerStats.playTime = playerStats.playTime;
            playerStats.upgrades.Clone(result.playerStats.upgrades);

            powerEggs.Clone(result.powerEggs);
            wishbones.Clone(result.wishbones);
            hensRescued.Clone(result.hensRescued);
            globalChanges.Clone(result.globalChanges);
            foreach (var area in AreaRegistry.GetAll())
                areaChanges[area].Clone(result.areaChanges[area]);

            return result;
        }


        public static class IO
        {
            public static SaveFile file = new();

            public static int fileID = -1;
            public static string fileRoot;

            public static JsonFile playerFile;
            //Contains location and playerStats.

            public static JsonFile worldChangesFile;
            //Contains powerEggs, hensRescued, and globalChanges

            public static Dictionary<AreaAsset, JsonFile> areaChangesFiles;

            public static void SetFileTarget(int fileID)
            {
                IO.fileID = fileID;
                fileRoot = Path.Combine(UnityEngine.Application.persistentDataPath, "Saves", $"File{fileID}");

                playerFile = new JsonFile(fileRoot, "playerData");
                worldChangesFile = new JsonFile(fileRoot, "worldChanges");
                areaChangesFiles = new();
                foreach (var area in AreaRegistry.GetAll())
                {
                    areaChangesFiles.Add(area, new JsonFile(fileRoot, $"flags_{area.name}"));
                }
            }

            public static void ClearFileTarget()
            {
                fileID = -1;
                fileRoot = null;
                playerFile = null;
                worldChangesFile = null;
                areaChangesFiles = null;
            }

            public static JsonFile.LoadResult Load()
            {
                if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");

                JsonFile.LoadResult result;
                result = playerFile.LoadFromFile();
                if (result != JsonFile.LoadResult.Success) return result;
                result = worldChangesFile.LoadFromFile();
                if (result != JsonFile.LoadResult.Success) return result;
                foreach (var pair in areaChangesFiles)
                {
                    result = pair.Value.LoadFromFile();
                    if (result != JsonFile.LoadResult.Success) return result;
                }


                if ((string)playerFile.Data["FileVersion"] != targetFileVersion)
                {
                    UnityEngine.Debug.LogWarning($"Save file version mismatch. Expected {targetFileVersion}, found {(string)playerFile.Data["FileVersion"]}. Attempting to load anyway.");
                }

                file.location = Destination.Deserialize(playerFile.Data[nameof(file.location)]);
                file.playerStats.maxHealth = (int)playerFile.Data[nameof(SavedPlayerStats.maxHealth)];
                file.playerStats.maxAmmo = (int)playerFile.Data[nameof(SavedPlayerStats.maxAmmo)];
                file.playerStats.currency = (int)playerFile.Data[nameof(SavedPlayerStats.currency)];
                file.playerStats.playTime = TimeSpan.Parse((string)playerFile.Data[nameof(SavedPlayerStats.playTime)]);

                JToken upgradesLoad = playerFile.Data[nameof(SavedPlayerStats.upgrades)];
                file.playerStats.upgrades = upgradesLoad.ToObject<Upgrades>();

                JToken powerEggsLoad = worldChangesFile.Data[nameof(file.powerEggs)];
                JToken wishbonesLoad = worldChangesFile.Data[nameof(file.wishbones)];
                JToken hensRescuedLoad = worldChangesFile.Data[nameof(file.hensRescued)];
                JToken globalChangesLoad = worldChangesFile.Data[nameof(file.globalChanges)];

                file.powerEggs.total = (int)powerEggsLoad[nameof(SavedCollectible.total)];
                for (int i = 0; i < file.powerEggs.isCollected.Count; i++)
                    file.powerEggs.isCollected[i] = (bool)powerEggsLoad[nameof(SavedCollectible.isCollected)][i];

                file.wishbones.total = (int)wishbonesLoad[nameof(SavedCollectible.total)];
                for (int i = 0; i < file.wishbones.isCollected.Count; i++)
                    file.wishbones.isCollected[i] = (bool)wishbonesLoad[nameof(SavedCollectible.isCollected)][i];

                file.hensRescued.total = (int)hensRescuedLoad[nameof(SavedCollectible.total)];
                for (int i = 0; i < file.hensRescued.isCollected.Count; i++)
                    file.hensRescued.isCollected[i] = (bool)hensRescuedLoad[nameof(SavedCollectible.isCollected)][i];

                file.globalChanges.LoadFromJson(globalChangesLoad);

                foreach (var area in AreaRegistry.GetAll())
                    file.areaChanges[area].LoadFromJson(areaChangesFiles[area].Data);

                return JsonFile.LoadResult.Success;
            }

            public static JsonFile.FileState Save()
            {
                if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");

                playerFile.Data = new JObject
                {
                    ["FileVersion"] = targetFileVersion,
                    [nameof(file.location)] = file.location.Serialize(nameof(file.location)),
                    [nameof(SavedPlayerStats.maxHealth)] = file.playerStats.maxHealth,
                    [nameof(SavedPlayerStats.maxAmmo)] = file.playerStats.maxAmmo,
                    [nameof(SavedPlayerStats.currency)] = file.playerStats.currency,
                    [nameof(SavedPlayerStats.playTime)] = file.playerStats.playTime.ToString(),
                    [nameof(SavedPlayerStats.upgrades)] = JObject.FromObject(file.playerStats.upgrades)
                };

                worldChangesFile.Data = new JObject
                {
                    [nameof(file.powerEggs)] = new JObject
                    {
                        [nameof(SavedCollectible.total)] = file.powerEggs.total,
                        [nameof(SavedCollectible.isCollected)] = new JArray(file.powerEggs.isCollected)
                    },
                    [nameof(file.wishbones)] = new JObject
                    {
                        [nameof(SavedCollectible.total)] = file.wishbones.total,
                        [nameof(SavedCollectible.isCollected)] = new JArray(file.wishbones.isCollected)
                    },
                    [nameof(file.hensRescued)] = new JObject
                    {
                        [nameof(SavedCollectible.total)] = file.hensRescued.total,
                        [nameof(SavedCollectible.isCollected)] = new JArray(file.hensRescued.isCollected)
                    },
                    [nameof(file.globalChanges)] = file.globalChanges.flags != null
                                                        ? JObject.FromObject(file.globalChanges.flags)
                                                        : new JObject()
                };

                // Save areaChanges to areaChangesFiles
                foreach (var area in AreaRegistry.GetAll())
                {
                    areaChangesFiles[area].Data = file.areaChanges[area].flags != null
                        ? JObject.FromObject(file.areaChanges[area].flags)
                        : new JObject();
                }

                // Save all files
                JsonFile.FileState state = playerFile.SaveToFile();
                if (state != JsonFile.FileState.Valid) return state;
                state = worldChangesFile.SaveToFile();
                if (state != JsonFile.FileState.Valid) return state;
                foreach (var pair in areaChangesFiles)
                {
                    state = pair.Value.SaveToFile();
                    if (state != JsonFile.FileState.Valid) return state;
                }

                return JsonFile.FileState.Valid;
            }
        }

    }
}
