using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RageRooster.Core.Save;
using RageRooster.World;
using UnityEngine;
using SLS.SaveFileCore;

namespace RageRooster.TOP.Save.Streams
{
    /// <summary>
    /// This is the 1.0.0 version of the Save Stream. Outdated but necessary for loading old save files. This version is no longer used for saving.
    /// </summary>
    public class FileVersion_UCF : FileVersion
    {
        public const string NUM = "UCF_Release";
        public override string version => NUM;

        static SaveData Transfer => SaveManager.TransferSnapshot;

        public FileVersion_UCF(int fileID) : base(fileID) { }
        public override void Initialize()
        {
            this.fileID = fileID;
            saveRootPath = $"{Application.persistentDataPath}/Save{fileID}";

            RootFile = new(saveRootPath, $"playerData");
            WorldChangesFile = new(saveRootPath, $"worldChanges");
            areaChangesFiles = new();
            foreach (string area in IDestination.AllAreas)
                areaChangesFiles.Add(area, new JsonFile(saveRootPath, $"flags_{area}"));
        }

        public JsonFile PlayerFile => RootFile;
        public JsonFile WorldChangesFile;
        public Dictionary<string, JsonFile> areaChangesFiles;


        #region Getters
        public override bool PathExists => RootFile.PathExists && WorldChangesFile.PathExists && areaChangesFiles.Values.All(x => x.PathExists);
        public override bool FileExists => RootFile.FileExists && WorldChangesFile.FileExists && areaChangesFiles.Values.All(x => x.FileExists);
        public override bool Exists => PathExists && FileExists;
        public override bool HasData => RootFile.HasData && WorldChangesFile.HasData && areaChangesFiles.Values.All(x => x.HasData);
        public override bool Valid => Exists && HasData;
        #endregion

        public override FileOpMessage LoadFromFile()
        {
            FileOpMessage result = FileOpMessage.Success;
            if (PlayerFile.LoadFromFile(out JObject PlayerData).IfFail(out result)) return result;
            if (WorldChangesFile.LoadFromFile(out JObject WorldChangesData).IfFail(out result)) return result;
            Dictionary<string, JObject> areaChangesData = new();
            foreach (var pair in areaChangesFiles)
            {
                JObject iAreaChangeData;
                if(pair.Value.LoadFromFile(out iAreaChangeData).IfFail(out result)) return result;
                areaChangesData.Add(pair.Key, iAreaChangeData);
            }

            Transfer.playerStats.location = (DestinationMap)PlayerData["location"];
            Transfer.playerStats.MaxHealth &= (int)PlayerData["maxHealth"];
            Transfer.playerStats.MaxAmmo &= (int)PlayerData["maxAmmo"];
            Transfer.playerStats.dropLaunch = (bool)PlayerData["upgrades"]["dropLaunch"];
            Transfer.playerStats.wallJump = (bool)PlayerData["upgrades"]["wallJump"];
            Transfer.playerStats.hellcopter = (bool)PlayerData["upgrades"]["hellcopter"];
            Transfer.playerStats.ragingCharge = (bool)PlayerData["upgrades"]["ragingCharge"];

            Transfer.progress.Currency &= (int)PlayerData["currency"];
            Transfer.progress.playTime = TimeSpan.Parse((string)PlayerData["playTime"]);

            Transfer.progress.powerEggs.collected = (int)WorldChangesData["powerEggs"]["total"];
            for (int i = 0; i < Transfer.progress.powerEggs.isCollected.Count; i++)
                Transfer.progress.powerEggs.isCollected[i] = (bool)WorldChangesData["powerEggs"]["isCollected"][i];

            Transfer.progress.wishbones.collected = (int)WorldChangesData["wishbones"]["total"];
            for (int i = 0; i < Transfer.progress.powerEggs.isCollected.Count; i++)
                Transfer.progress.wishbones.isCollected[i] = (bool)WorldChangesData["wishbones"]["isCollected"][i];

            Transfer.progress.hensRescued.collected = (int)WorldChangesData["hensRescued"]["total"];
            for (int i = 0; i < Transfer.progress.powerEggs.isCollected.Count; i++)
                Transfer.progress.hensRescued.isCollected[i] = (bool)WorldChangesData["hensRescued"]["isCollected"][i];

            JObject globalChangesLoad = (JObject)WorldChangesData["globalChanges"];

            Transfer.flags["Global"].LoadFromJson(globalChangesLoad);

            foreach (var item in areaChangesData)
                if (Transfer.flags.ContainsKey(item.Key))
                    Transfer.flags[item.Key].LoadFromJson(item.Value);

            return result;
        }
        public override FileOpMessage SaveToFile()
        {
            //NO

            //PlayerData = new JObject
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
            //WorldChangesData = new JObject
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
            throw new InvalidOperationException();
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

        public override void ExportDisplayData(out object resultF)
        {
            PlayerFile.LoadFromFile(out JObject PlayerData);
            WorldChangesFile.LoadFromFile(out JObject ProgressData);

            TimeSpan readTime = TimeSpan.Parse((string)PlayerData["playTime"]);
            DestinationMap readLocation = PlayerData["location"];
            SaveData.MenuDisplayData result = new()
            {
                timeString = $"{(int)readTime.TotalHours}:{readTime.Minutes:D2}:{readTime.Seconds:D2}",
                location = readLocation,
                completionPercentage = GetCompletionPercentage(),
                health = (int)PlayerData["maxHealth"],
                powerEggs = (int)ProgressData["powerEggs"]["total"],
                hensRescued = (int)ProgressData["hensRescued"]["total"],
            };
            resultF = result;
        }

        public override void DeleteFile()
        {
            PlayerFile.DeleteFile();
            WorldChangesFile.DeleteFile();
            foreach (var item in areaChangesFiles.Values) item.DeleteFile();
        }
    }
}
