using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using RageRooster.Core.Save;
using RageRooster.TOP.Save;
using RageRooster.World;
using Utilities.JSON;

namespace RageRooster.TOP.Save.Streams
{
    public class SaveStream150 : SaveIOStream
    {
        public override float version => 1.5f;

        JsonFile PlayerFile;
        JsonFile ProgressFile;
        JsonFile WorldChangesFile;

        public SaveStream150(int fileID, out JsonFile.FileState state) : base(fileID, out state)
        {
            this.fileID = fileID;
            saveRootPath = $"{UnityEngine.Application.persistentDataPath}/Save{fileID}";

            PlayerFile = new(saveRootPath, $"PlayerData");
            ProgressFile = new(saveRootPath, $"Progress");
            WorldChangesFile = new(saveRootPath, $"WorldChanges");

            RootFile = PlayerFile;
            SecondaryFiles = new JsonFile[]
            {
                ProgressFile,
                WorldChangesFile,
            };


        }

        protected override JsonFile.FileState ReadData()
        {

        }
        protected override JsonFile.FileState WriteData()
        {
            PlayerFile.Data = new()
            {
                ["MaxHealth"] = Data.playerStats.MaxHealth.Value,
                ["MaxAmmo"] = Data.playerStats.MaxAmmo.Value,
                ["Location"] = (JToken)Data.playerStats.location,
                ["Upgrades"] = new JObject
                {
                    ["DropLaunch"] = Data.playerStats.dropLaunch,
                    ["WallJump"] = Data.playerStats.wallJump,
                    ["Hellcopter"] = Data.playerStats.hellcopter,
                    ["RagingCharge"] = Data.playerStats.ragingCharge,
                    ["Glide"] = Data.playerStats.glide,
                    ["DoubleJump"] = Data.playerStats.doubleJump,
                    ["Lasso"] = Data.playerStats.lasso,
                }
            };
            ProgressFile.Data = new()
            {
                ["PlayTime"] = Data.progress.playTime,
                ["Completion"] = Data.Completion,
                ["PowerEggs"] = Data.progress.powerEggs.collected,
                ["HensRescued"] = Data.progress.hensRescued.collected,
                ["Wishbones"] = Data.progress.wishbones.collected,
                ["PowerEggIDs"] = new JArray(Data.progress.powerEggs.isCollected),
                ["HensRescuedIDs"] = new JArray(Data.progress.hensRescued.isCollected),
                ["WishboneIDs"] = new JArray(Data.progress.wishbones.isCollected),
            };
            WorldChangesFile.Data = new();
            WorldChangesFile.Data.Add("Global", Data.globalChanges);
            foreach (string key in IDestination.AllAreas)
                WorldChangesFile.Data.Add(key, Data.areaChanges[key]);
        }

        public float ApproxCompletion()
        {
            if (ProgressFile.State != JsonFile.FileState.Valid) return 0f;
            return (float)ProgressFile.Data["Completion"];
        }

        public override void ExportMenuDisplayData(out SaveData.MenuDisplayData result)
        {
            TimeSpan readTime = TimeSpan.Parse((string)PlayerFile.Data["playTime"]);
            DestinationMap readLocation = PlayerFile.Data["location"];
            result = new SaveData.MenuDisplayData
            {
                timeString = $"{(int)readTime.TotalHours}:{readTime.Minutes:D2}:{readTime.Seconds:D2}",
                locationString = readLocation.ToString(),
                completionPercentage = ApproxCompletion(),
                health = (int)PlayerFile.Data["maxHealth"],
                powerEggs = (int)WorldChangesFile.Data["powerEggs"]["total"],
                hensRescued = (int)WorldChangesFile.Data["hensRescued"]["total"],
            };
        }
    }
}
