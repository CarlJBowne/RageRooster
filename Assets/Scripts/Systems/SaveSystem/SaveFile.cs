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
        public const string targetFileVersion = "1.0.0";
        public TransitionDestination location;
        public SavedPlayerStats playerStats = new();
        public PowerEggs powerEggs = new();
        public Wishbones wishbones = new();
        public SavedHens hensRescued = new();
        public SavedFlagSet globalChanges = new();
        public Dictionary<AreaAsset, SavedFlagSet> areaChanges = new();

        public class SavedPlayerStats
        {
            public int maxHealth = 3;
            public int maxAmmo = 0;
            public int currency = 0;
            public TimeSpan playTime = TimeSpan.Zero;
            public Dictionary<string, bool> upgrades = new();
        }

        public class PowerEggs
        {
            public int total = 0;
            public List<bool> isCollected;
        }

        public class Wishbones
        {
            public int total = 0;
            public List<bool> isCollected;
        }

        public class SavedHens
        {
            public int total = 0;
            public List<bool> isCollected;
        }

        /// <summary>
        /// Default Constructor, Clones data from default files.
        /// </summary>
        public SaveFile()
        {
            location = new();
            location.area = AreaRegistry.GetAll()[0];
            location.room = location.area.rooms[0];
            location.spawnID = 0;
            playerStats.upgrades = SavedValueManager.Upgrades; // Check Validity later.
            powerEggs.isCollected = new(new bool[SavedValueManager.PowerEggs.Count]);
            hensRescued.isCollected = new(new bool[SavedValueManager.HensRescued.Count]);
            wishbones.isCollected = new(new bool[SavedValueManager.Wishbones.Count]);
            globalChanges = UnityEngine.Object.Instantiate(SavedValueManager.GlobalFlagDefaults);
            foreach (var area in AreaRegistry.GetAll()) 
                areaChanges.Add(area, UnityEngine.Object.Instantiate(area.flagDefaults));
        }

        public JsonFile.LoadResult Load()
        {
            JsonFile.LoadResult result = IO.LoadFile();
            if (result != JsonFile.LoadResult.Success) return result;

            if((string)IO.playerFile.Data["FileVersion"] != targetFileVersion)
            {
                UnityEngine.Debug.LogWarning($"Save file version mismatch. Expected {targetFileVersion}, found {(string)IO.playerFile.Data["FileVersion"]}. Attempting to load anyway.");
            }

            location = TransitionDestination.Deserialize(IO.playerFile.Data[nameof(location)]);
            playerStats.maxHealth = (int)IO.playerFile.Data[nameof(SavedPlayerStats.maxHealth)];
            playerStats.maxAmmo = (int)IO.playerFile.Data[nameof(SavedPlayerStats.maxAmmo)];
            playerStats.currency = (int)IO.playerFile.Data[nameof(SavedPlayerStats.currency)];
            playerStats.playTime = TimeSpan.Parse((string)IO.playerFile.Data[nameof(SavedPlayerStats.playTime)]);

            JToken upgradesLoad = IO.playerFile.Data[nameof(SavedPlayerStats.upgrades)];
            foreach (var ID in playerStats.upgrades.Keys) 
                playerStats.upgrades[ID] = (bool)upgradesLoad[ID];

            JToken powerEggsLoad = IO.worldChangesFile.Data[nameof(powerEggs)];
            JToken wishbonesLoad = IO.worldChangesFile.Data[nameof(wishbones)];
            JToken hensRescuedLoad = IO.worldChangesFile.Data[nameof(hensRescued)];
            JToken globalChangesLoad = IO.worldChangesFile.Data[nameof(globalChanges)];

            powerEggs.total = (int)powerEggsLoad[nameof(PowerEggs.total)];
            for (int i = 0; i < powerEggs.isCollected.Count; i++)
                powerEggs.isCollected[i] = (bool)powerEggsLoad[nameof(PowerEggs.isCollected)][i];

            wishbones.total = (int)wishbonesLoad[nameof(PowerEggs.total)];
            for (int i = 0; i < wishbones.isCollected.Count; i++)
                wishbones.isCollected[i] = (bool)wishbonesLoad[nameof(PowerEggs.isCollected)][i];

            hensRescued.total = (int)hensRescuedLoad[nameof(PowerEggs.total)];
            for (int i = 0; i < hensRescued.isCollected.Count; i++)
                hensRescued.isCollected[i] = (bool)hensRescuedLoad[nameof(PowerEggs.isCollected)][i];
            
            globalChanges.LoadFromJson(globalChangesLoad);

            foreach (var area in AreaRegistry.GetAll())
                areaChanges[area].LoadFromJson(IO.areaChangesFiles[area].Data);

            return JsonFile.LoadResult.Success;
        }

        public JsonFile.FileState Save()
        {
            if(IO.fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");

            IO.playerFile.Data = new JObject
            {
                ["FileVersion"] = targetFileVersion,
                [nameof(location)] = location.Serialize(nameof(location)),
                [nameof(SavedPlayerStats.maxHealth)] = playerStats.maxHealth,
                [nameof(SavedPlayerStats.maxAmmo)] = playerStats.maxAmmo,
                [nameof(SavedPlayerStats.currency)] = playerStats.currency,
                [nameof(SavedPlayerStats.playTime)] = playerStats.playTime.ToString(),
                [nameof(SavedPlayerStats.upgrades)] = JObject.FromObject(playerStats.upgrades)
            };

            IO.worldChangesFile.Data = new JObject
            {
                [nameof(powerEggs)] = new JObject
                {
                    [nameof(PowerEggs.total)] = powerEggs.total,
                    [nameof(PowerEggs.isCollected)] = new JArray(powerEggs.isCollected)
                },
                [nameof(wishbones)] = new JObject
                {
                    [nameof(PowerEggs.total)] = wishbones.total,
                    [nameof(PowerEggs.isCollected)] = new JArray(wishbones.isCollected)
                },
                [nameof(hensRescued)] = new JObject
                {
                    [nameof(PowerEggs.total)] = hensRescued.total,
                    [nameof(PowerEggs.isCollected)] = new JArray(hensRescued.isCollected)
                },
                [nameof(globalChanges)] = globalChanges.flags != null
                                            ? JObject.FromObject(globalChanges.flags)
                                            : new JObject()
            };

            // Save areaChanges to areaChangesFiles
            foreach (var area in AreaRegistry.GetAll())
            {
                IO.areaChangesFiles[area].Data = areaChanges[area].flags != null
                    ? JObject.FromObject(areaChanges[area].flags)
                    : new JObject();
            }

            // Save all files
            JsonFile.FileState state = IO.playerFile.SaveToFile();
            if (state != JsonFile.FileState.Valid) return state;
            state = IO.worldChangesFile.SaveToFile();
            if (state != JsonFile.FileState.Valid) return state;
            foreach (var pair in IO.areaChangesFiles)
            {
                state = pair.Value.SaveToFile();
                if (state != JsonFile.FileState.Valid) return state;
            }

            return JsonFile.FileState.Valid;
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

            public static JsonFile.LoadResult LoadFile()
            {
                if(fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");

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
                return result;
            }
        }
    }
}
