using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using RageRooster.Core.Save;
using RageRooster.World;
using UnityEngine;
using Utilities.JSON;

namespace RageRooster.TOP.Save.Streams
{
    /// <summary>
    /// This is the 1.0.0 version of the Save Stream. Outdated but necessary for loading old save files. This version is no longer used for saving.
    /// </summary>
    public class SaveStream100 : SaveIOStream
    {
        public override float version => -1.00f;

        public SaveStream100(int fileID, out JsonFile.FileState state) : base(fileID, out state)
        {
            this.fileID = fileID;
            saveRootPath = $"{Application.persistentDataPath}/Save{fileID}";

            RootFile = new(saveRootPath, $"playerData");
            WorldChangesFile = new(saveRootPath, $"worldChanges");
            areaChangesFiles = new();
            foreach (string area in IDestination.AllAreas)
                areaChangesFiles.Add(area, new JsonFile(saveRootPath, $"flags_{area}"));
            SecondaryFiles = areaChangesFiles.Values.Append(WorldChangesFile).ToArray();

            if (RootFile.State != JsonFile.FileState.Valid)
            {
                state = RootFile.State;
                return;
            }
            if (WorldChangesFile.State != JsonFile.FileState.Valid)
            {
                state = WorldChangesFile.State;
                return;
            }
            foreach (var item in areaChangesFiles)
            {
                if (item.Value.State != JsonFile.FileState.Valid)
                {
                    state = item.Value.State;
                    return;
                }
            }
            state = JsonFile.FileState.Valid;

        }

        public JsonFile PlayerFile => RootFile;

        //Contains powerEggs, hensRescued, and globalChanges
        public JsonFile WorldChangesFile;

        public Dictionary<string, JsonFile> areaChangesFiles;

        protected override JsonFile.FileState ReadData()
        {
            Transfer.playerStats.location = (DestinationMap)PlayerFile.Data["location"];
            Transfer.playerStats.MaxHealth &= (int)PlayerFile.Data["maxHealth"];
            Transfer.playerStats.MaxAmmo &= (int)PlayerFile.Data["maxAmmo"];
            Transfer.playerStats.dropLaunch = (bool)PlayerFile.Data["upgrades"]["dropLaunch"];
            Transfer.playerStats.wallJump = (bool)PlayerFile.Data["upgrades"]["wallJump"];
            Transfer.playerStats.hellcopter = (bool)PlayerFile.Data["upgrades"]["hellcopter"];
            Transfer.playerStats.ragingCharge = (bool)PlayerFile.Data["upgrades"]["ragingCharge"];

            Transfer.progress.Currency &= (int)PlayerFile.Data["currency"];
            Transfer.progress.playTime = TimeSpan.Parse((string)PlayerFile.Data["playTime"]);

            Transfer.progress.powerEggs.collected = (int)WorldChangesFile.Data["powerEggs"]["total"];
            for (int i = 0; i < Transfer.progress.powerEggs.isCollected.Count; i++)
                Transfer.progress.powerEggs.isCollected[i] = (bool)WorldChangesFile.Data["powerEggs"]["isCollected"][i];

            Transfer.progress.wishbones.collected = (int)WorldChangesFile.Data["wishbones"]["total"];
            for (int i = 0; i < Transfer.progress.powerEggs.isCollected.Count; i++)
                Transfer.progress.wishbones.isCollected[i] = (bool)WorldChangesFile.Data["wishbones"]["isCollected"][i];

            Transfer.progress.hensRescued.collected = (int)WorldChangesFile.Data["hensRescued"]["total"];
            for (int i = 0; i < Transfer.progress.powerEggs.isCollected.Count; i++)
                Transfer.progress.hensRescued.isCollected[i] = (bool)WorldChangesFile.Data["hensRescued"]["isCollected"][i];

            JObject globalChangesLoad = (JObject)WorldChangesFile.Data["globalChanges"];

            Transfer.areaChanges["GLOBAL"].LoadFromJson(globalChangesLoad);

            foreach (var item in areaChangesFiles)
                if (Transfer.areaChanges.ContainsKey(item.Key))
                    Transfer.areaChanges[item.Key].LoadFromJson(item.Value.Data as JObject);

            return JsonFile.FileState.Valid;
        }
        protected override JsonFile.FileState WriteData()
        {
            //NO

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
            return 0;
            int totalCollectibles = 0 // SavedValueRegistry.PowerEggs.Count + SavedValueRegistry.Wishbones.Count + SavedValueRegistry.HensRescued.Count
                                      ;
            if (totalCollectibles == 0) return 100f;
            int collected = 0;
            //collected += WorldChangesFile[nameof(powerEggs)][nameof(SavedCollectible.total)].ToObject<int>();
            //collected += WorldChangesFile[nameof(wishbones)][nameof(SavedCollectible.total)].ToObject<int>();
            //collected += WorldChangesFile[nameof(hensRescued)][nameof(SavedCollectible.total)].ToObject<int>();

            return (collected / (float)totalCollectibles) * 100f;
        }

        public override void ExportMenuDisplayData(out SaveData.MenuDisplayData result)
        {
            TimeSpan readTime = TimeSpan.Parse((string)PlayerFile.Data["playTime"]);
            DestinationMap readLocation = PlayerFile.Data["location"];
            result = new SaveData.MenuDisplayData
            {
                timeString = $"{(int)readTime.TotalHours}:{readTime.Minutes:D2}:{readTime.Seconds:D2}",
                location = readLocation,
                completionPercentage = GetCompletionPercentage(),
                health = (int)PlayerFile.Data["maxHealth"],
                powerEggs = (int)WorldChangesFile.Data["powerEggs"]["total"],
                hensRescued = (int)WorldChangesFile.Data["hensRescued"]["total"],
            };
        }
    }
}
