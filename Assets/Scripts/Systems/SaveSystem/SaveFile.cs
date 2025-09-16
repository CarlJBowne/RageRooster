using Newtonsoft.Json.Linq;
using RageRooster.RoomSystem;
using System;
using System.IO;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace RageRooster.Systems.SaveSystem
{
    public class SaveFile
    {
        public static SaveFile Current;


        public const string targetFileVersion = "1.0.0";
        public TransitionDestination location;
        public SavedPlayerStats playerStats = new();
        public SavedCollectible powerEggs = new();
        public SavedCollectible wishbones = new();
        public SavedCollectible hensRescued = new();
        public SavedFlagSet globalChanges;
        public Dictionary<AreaAsset, SavedFlagSet> areaChanges = new();

        public class SavedPlayerStats
        {
            public int maxHealth = 3;
            public int maxAmmo = 0;
            public int currency = 0;
            public TimeSpan playTime = TimeSpan.Zero;
            public Dictionary<string, bool> upgrades = new();
        }

        public class SavedCollectible
        {
            public int total = 0;
            public List<bool> isCollected;
        }

        /// <summary>
        /// Default Constructor, Clones data from default files.
        /// </summary>
        public SaveFile()
        {
            location = TransitionDestination.StartingDefault();
            playerStats.upgrades = SavedValueManager.Upgrades; // Check Validity later.
            powerEggs.isCollected = new(new bool[SavedValueManager.PowerEggs.Count]);
            hensRescued.isCollected = new(new bool[SavedValueManager.HensRescued.Count]);
            wishbones.isCollected = new(new bool[SavedValueManager.Wishbones.Count]);
            globalChanges = UnityEngine.Object.Instantiate(SavedValueManager.GlobalFlagDefaults);
            foreach (var area in AreaRegistry.GetAll()) 
                areaChanges.Add(area, UnityEngine.Object.Instantiate(area.flagDefaults));
        }

        public static class IO
        {
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

            public static JsonFile.LoadResult Load(SaveFile saveFile)
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

                saveFile.location = TransitionDestination.Deserialize(playerFile.Data[nameof(saveFile.location)]);
                saveFile.playerStats.maxHealth = (int)playerFile.Data[nameof(SavedPlayerStats.maxHealth)];
                saveFile.playerStats.maxAmmo = (int)playerFile.Data[nameof(SavedPlayerStats.maxAmmo)];
                saveFile.playerStats.currency = (int)playerFile.Data[nameof(SavedPlayerStats.currency)];
                saveFile.playerStats.playTime = TimeSpan.Parse((string)playerFile.Data[nameof(SavedPlayerStats.playTime)]);

                JToken upgradesLoad = playerFile.Data[nameof(SavedPlayerStats.upgrades)];
                foreach (var ID in saveFile.playerStats.upgrades.Keys)
                    saveFile.playerStats.upgrades[ID] = (bool)upgradesLoad[ID];

                JToken powerEggsLoad = worldChangesFile.Data[nameof(saveFile.powerEggs)];
                JToken wishbonesLoad = worldChangesFile.Data[nameof(saveFile.wishbones)];
                JToken hensRescuedLoad = worldChangesFile.Data[nameof(saveFile.hensRescued)];
                JToken globalChangesLoad = worldChangesFile.Data[nameof(saveFile.globalChanges)];

                saveFile.powerEggs.total = (int)powerEggsLoad[nameof(SavedCollectible.total)];
                for (int i = 0; i < saveFile.powerEggs.isCollected.Count; i++)
                    saveFile.powerEggs.isCollected[i] = (bool)powerEggsLoad[nameof(SavedCollectible.isCollected)][i];

                saveFile.wishbones.total = (int)wishbonesLoad[nameof(SavedCollectible.total)];
                for (int i = 0; i < saveFile.wishbones.isCollected.Count; i++)
                    saveFile.wishbones.isCollected[i] = (bool)wishbonesLoad[nameof(SavedCollectible.isCollected)][i];

                saveFile.hensRescued.total = (int)hensRescuedLoad[nameof(SavedCollectible.total)];
                for (int i = 0; i < saveFile.hensRescued.isCollected.Count; i++)
                    saveFile.hensRescued.isCollected[i] = (bool)hensRescuedLoad[nameof(SavedCollectible.isCollected)][i];

                saveFile.globalChanges.LoadFromJson(globalChangesLoad);

                foreach (var area in AreaRegistry.GetAll())
                    saveFile.areaChanges[area].LoadFromJson(areaChangesFiles[area].Data);

                return JsonFile.LoadResult.Success;
            }

            public static JsonFile.FileState Save(SaveFile saveFile)
            {
                if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");

                playerFile.Data = new JObject
                {
                    ["FileVersion"] = targetFileVersion,
                    [nameof(saveFile.location)] = saveFile.location.Serialize(nameof(saveFile.location)),
                    [nameof(SavedPlayerStats.maxHealth)] = saveFile.playerStats.maxHealth,
                    [nameof(SavedPlayerStats.maxAmmo)] = saveFile.playerStats.maxAmmo,
                    [nameof(SavedPlayerStats.currency)] = saveFile.playerStats.currency,
                    [nameof(SavedPlayerStats.playTime)] = saveFile.playerStats.playTime.ToString(),
                    [nameof(SavedPlayerStats.upgrades)] = JObject.FromObject(saveFile.playerStats.upgrades)
                };

                worldChangesFile.Data = new JObject
                {
                    [nameof(saveFile.powerEggs)] = new JObject
                    {
                        [nameof(SavedCollectible.total)] = saveFile.powerEggs.total,
                        [nameof(SavedCollectible.isCollected)] = new JArray(saveFile.powerEggs.isCollected)
                    },
                    [nameof(saveFile.wishbones)] = new JObject
                    {
                        [nameof(SavedCollectible.total)] = saveFile.wishbones.total,
                        [nameof(SavedCollectible.isCollected)] = new JArray(saveFile.wishbones.isCollected)
                    },
                    [nameof(saveFile.hensRescued)] = new JObject
                    {
                        [nameof(SavedCollectible.total)] = saveFile.hensRescued.total,
                        [nameof(SavedCollectible.isCollected)] = new JArray(saveFile.hensRescued.isCollected)
                    },
                    [nameof(saveFile.globalChanges)] = saveFile.globalChanges.flags != null
                                                        ? JObject.FromObject(saveFile.globalChanges.flags)
                                                        : new JObject()
                };

                // Save areaChanges to areaChangesFiles
                foreach (var area in AreaRegistry.GetAll())
                {
                    areaChangesFiles[area].Data = saveFile.areaChanges[area].flags != null
                        ? JObject.FromObject(saveFile.areaChanges[area].flags)
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
