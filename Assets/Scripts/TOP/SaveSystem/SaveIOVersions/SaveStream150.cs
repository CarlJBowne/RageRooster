using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using RageRooster.Actors.Save.Collectibles;
using RageRooster.Core.Save;
using RageRooster.TOP.Save;
using RageRooster.World;
using Utilities.JSON;

namespace RageRooster.TOP.Save.Streams
{
    public class SaveStream150 : SaveIOStream
    {
        public override float version => 1.5f;

        readonly JsonFile PlayerFile;
        readonly JsonFile ProgressFile;
        readonly JsonFile WorldChangesFile;

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

            state = State;
        }

        protected override JsonFile.FileState ReadData()
        {
            Transfer.playerStats.MaxHealth &= (int)PlayerFile["MaxHealth"];
            Transfer.playerStats.MaxAmmo &= (int)PlayerFile["MaxAmmo"];
            Transfer.playerStats.location = (DestinationMap)PlayerFile["Location"];
            Transfer.playerStats.dropLaunch   = (bool)PlayerFile["Upgrades"]["DropLaunch"];
            Transfer.playerStats.wallJump     = (bool)PlayerFile["Upgrades"]["WallJump"];
            Transfer.playerStats.hellcopter   = (bool)PlayerFile["Upgrades"]["Hellcopter"];
            Transfer.playerStats.ragingCharge = (bool)PlayerFile["Upgrades"]["RagingCharge"];
            Transfer.playerStats.glide        = (bool)PlayerFile["Upgrades"]["Glide"];
            Transfer.playerStats.doubleJump   = (bool)PlayerFile["Upgrades"]["DoubleJump"];
            Transfer.playerStats.lasso        = (bool)PlayerFile["Upgrades"]["Lasso"];

            Transfer.progress.playTime = TimeSpan.Parse(ProgressFile["PlayTime"].ToString());
            Load_SavedCollectible(Transfer.progress.powerEggs, 
                (JObject)ProgressFile["PowerEggs"], 
                (JArray)ProgressFile["PowerEggIDs"]);
            Load_SavedCollectible(Transfer.progress.wishbones, 
                (JObject)ProgressFile["Wishbones"], 
                (JArray)ProgressFile["WishboneIDs"]);
            Load_SavedCollectible(Transfer.progress.hensRescued, 
                (JObject)ProgressFile["HensRescued"], 
                (JArray)ProgressFile["HensRescuedIDs"]);

            return JsonFile.FileState.Valid;
        }
        protected override JsonFile.FileState WriteData()
        {
            PlayerFile.Data = new()
            {
                ["MaxHealth"] = Transfer.playerStats.MaxHealth.Value,
                ["MaxAmmo"] = Transfer.playerStats.MaxAmmo.Value,
                ["Location"] = (JToken)Transfer.playerStats.location,
                ["Upgrades"] = new JObject
                {
                    ["DropLaunch"] = Transfer.playerStats.dropLaunch,
                    ["WallJump"] = Transfer.playerStats.wallJump,
                    ["Hellcopter"] = Transfer.playerStats.hellcopter,
                    ["RagingCharge"] = Transfer.playerStats.ragingCharge,
                    ["Glide"] = Transfer.playerStats.glide,
                    ["DoubleJump"] = Transfer.playerStats.doubleJump,
                    ["Lasso"] = Transfer.playerStats.lasso,
                }
            };
            ProgressFile.Data = new()
            {
                ["PlayTime"] = Transfer.progress.playTime,
                ["Completion"] = Transfer.Completion,
                ["PowerEggs"] = Transfer.progress.powerEggs.collected,
                ["HensRescued"] = Transfer.progress.hensRescued.collected,
                ["Wishbones"] = Transfer.progress.wishbones.collected,
                ["PowerEggIDs"] = Save_SavedCollectible_IDs(Transfer.progress.powerEggs),
                ["HensRescuedIDs"] = Save_SavedCollectible_IDs(Transfer.progress.hensRescued),
                ["WishboneIDs"] = Save_SavedCollectible_IDs(Transfer.progress.wishbones),
                ["StoryFlags"] = null //This one's gonna be hard.
            };
            WorldChangesFile.Data = new()
            {
                ["Global"] = Transfer.globalChanges
            };
            foreach (string key in IDestination.AllAreas)
                WorldChangesFile.Data.Add(key, Transfer.areaChanges[key]);

            return JsonFile.FileState.Valid;
        }

        #region Load Helpers

        public static void Load_SavedCollectible(SavedCollectible coll, JObject integer, JArray array)
        {
            coll.collected = integer.ToObject<int>();
            for (int i = 0; i < array.Count; i++)
            {
                string id = array[i].ToString();
                if (coll.IDs.Contains(id)) coll.isCollected[coll.IDs.IndexOf(id)] = true;
            }
        }
        public static JArray Save_SavedCollectible_IDs(SavedCollectible coll)
        {
            JArray array = new();
            for (int i = 0; i < coll.IDs.Count; i++)
                if (coll.isCollected[i]) 
                    array.Add(coll.IDs[i]);
            return array;
        }

        #endregion

        public float ApproxCompletion()
        {
            if (ProgressFile.State != JsonFile.FileState.Valid) return 0f;
            return (float)ProgressFile.Data["Completion"];
        }

        public override void ExportMenuDisplayData(out SaveData.MenuDisplayData result)
        {
            TimeSpan readTime = TimeSpan.Parse((string)PlayerFile.Data["playTime"]);
            result = new SaveData.MenuDisplayData
            {
                timeString = $"{(int)readTime.TotalHours}:{readTime.Minutes:D2}:{readTime.Seconds:D2}",
                location = PlayerFile.Data["location"],
                completionPercentage = ApproxCompletion(),
                health = (int)PlayerFile.Data["maxHealth"],
                powerEggs = (int)WorldChangesFile.Data["powerEggs"]["total"],
                hensRescued = (int)WorldChangesFile.Data["hensRescued"]["total"],
            };
        }
    }
}
