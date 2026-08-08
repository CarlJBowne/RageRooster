using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RageRooster.Core.Save;
using RageRooster.World;
using Unity.VisualScripting;
using UnityEngine;
using Utilities.JSON;

namespace RageRooster.TOP.Save
{
    /// <summary>
    /// An Input Output stream for Saving/Loading Save Data to/from disk. Also used to display save files in UI.
    /// </summary>
    public class SaveFileIO : JsonStream
    {
        public static SaveFileIO Primary;

        public const string targetFileVersion = "1.0.0";

        public static void InitializeSaves(int fileNo)
        {
            Primary = new(fileNo);
            SaveData.SaveToSaveFile = SaveToSaveFile;
            SaveData.RevertToSaveFile = RevertToSaveFile;
            SaveData.InitializeSystem();
            RevertToSaveFile();
        }

        public static void SaveToSaveFile()
        {
            SaveData.Active.progress.playTime += TimeSpan.FromSeconds(SavedProgress.UpdateGameTime());
            SaveData.Clone(SaveData.Active, SaveData.DeathReloadData);
            SaveData.Clone(SaveData.Active, Primary.LoadOperator);
            Primary.SaveToFile();
        }
        public static void RevertToSaveFile()
        {
            SavedProgress.UpdateGameTime();
            Primary.LoadFromFile();
            SaveData.Clone(Primary.LoadOperator, SaveData.Active);
            SaveData.Clone(SaveData.Active, SaveData.DeathReloadData);
        }




        public SaveFileIO(int fileID)
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

}