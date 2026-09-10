using System;
using Newtonsoft.Json.Linq;
using RageRooster.Core.Save;
using RageRooster.World;
using SLS.SaveFileCore;

namespace RageRooster.TOP.Save.Streams
{
    public class FileVersion_10 : FileVersion
    {
        public const string NUM = "UNLEASHED 1.0";
        public override string version => NUM;

        static SaveData Transfer => SaveManager.TransferSnapshot;

        JsonFile PlayerFile => RootFile;
        JsonFile ProgressFile;
        JsonFile WorldChangesFile;

        public FileVersion_10(int fileID) : base(fileID) { }
        public override void Initialize()
        {
            this.fileID = fileID;
            saveRootPath = $"{UnityEngine.Application.persistentDataPath}/Save{fileID}";

            RootFile = new(saveRootPath, $"PlayerData");
            ProgressFile = new(saveRootPath, $"Progress");
            WorldChangesFile = new(saveRootPath, $"WorldChanges");

        }

        #region Getters
        public override bool PathExists => RootFile.PathExists && ProgressFile.PathExists && WorldChangesFile.PathExists;
        public override bool FileExists => RootFile.FileExists && ProgressFile.FileExists && WorldChangesFile.FileExists;
        public override bool Exists => PathExists && FileExists;
        public override bool HasData => RootFile.HasData && ProgressFile.HasData && WorldChangesFile.HasData;
        public override bool Valid => Exists && HasData;
        #endregion

        public override FileOpMessage LoadFromFile()
        {
            FileOpMessage result = FileOpMessage.Success;
            if (PlayerFile.LoadFromFile(out JObject PlayerData).IfFail(out result)) return result;
            if (ProgressFile.LoadFromFile(out JObject ProgressData).IfFail(out result)) return result;
            if (WorldChangesFile.LoadFromFile(out JObject WorldChangesData).IfFail(out result)) return result;

            Transfer.playerStats.MaxHealth &= (int)PlayerData["MaxHealth"];
            Transfer.playerStats.MaxAmmo &= (int)PlayerData["MaxAmmo"];
            Transfer.playerStats.location = (DestinationMap)PlayerData["Location"];
            Transfer.playerStats.dropLaunch = (bool)PlayerData["Upgrades"]["DropLaunch"];
            Transfer.playerStats.wallJump = (bool)PlayerData["Upgrades"]["WallJump"];
            Transfer.playerStats.hellcopter = (bool)PlayerData["Upgrades"]["Hellcopter"];
            Transfer.playerStats.ragingCharge = (bool)PlayerData["Upgrades"]["RagingCharge"];
            Transfer.playerStats.glide = (bool)PlayerData["Upgrades"]["Glide"];
            Transfer.playerStats.doubleJump = (bool)PlayerData["Upgrades"]["DoubleJump"];
            Transfer.playerStats.lasso = (bool)PlayerData["Upgrades"]["Lasso"];

            Transfer.progress.playTime = TimeSpan.Parse(ProgressData["PlayTime"].ToString());
            Load_SavedCollectible(Transfer.progress.powerEggs,
                (JObject)ProgressData["PowerEggs"],
                (JArray)ProgressData["PowerEggIDs"]);
            Load_SavedCollectible(Transfer.progress.wishbones,
                (JObject)ProgressData["Wishbones"],
                (JArray)ProgressData["WishboneIDs"]);
            Load_SavedCollectible(Transfer.progress.hensRescued,
                (JObject)ProgressData["HensRescued"],
                (JArray)ProgressData["HensRescuedIDs"]);
            Transfer.progress.storyFlags.LoadFromJson(ProgressData["StoryFlags"] as JArray);

            Transfer.flags["Global"].LoadFromJson(WorldChangesData["Global"] as JObject);
            foreach (string key in IDestination.AllAreas)
                Transfer.flags[key].LoadFromJson(WorldChangesData[key] as JObject);

            return result;
        }
        public override FileOpMessage SaveToFile()
        {
            PlayerFile.MakeBackup();
            ProgressFile.MakeBackup();
            WorldChangesFile.MakeBackup();

            try
            {
                PlayerFile.SaveToFile(new()
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
                });

                ProgressFile.SaveToFile(new()
                {
                    ["PlayTime"] = Transfer.progress.playTime,
                    ["Completion"] = Transfer.Completion,
                    ["PowerEggs"] = Transfer.progress.powerEggs.collected,
                    ["HensRescued"] = Transfer.progress.hensRescued.collected,
                    ["Wishbones"] = Transfer.progress.wishbones.collected,
                    ["PowerEggIDs"] = Save_SavedCollectible_IDs(Transfer.progress.powerEggs),
                    ["HensRescuedIDs"] = Save_SavedCollectible_IDs(Transfer.progress.hensRescued),
                    ["WishboneIDs"] = Save_SavedCollectible_IDs(Transfer.progress.wishbones),
                    ["StoryFlags"] = Transfer.progress.storyFlags.SaveToJson()
                });

                WorldChangesFile.SaveToFile(new JObject().Populate(o =>
                {
                    o.Add("Global", Transfer.flags["Global"].SaveToJson());
                    foreach (string key in IDestination.AllAreas)
                        o.Add(key, Transfer.flags[key].SaveToJson());
                }));

                return FileOpMessage.Success;
            }
            catch (Exception)
            {
                SaveErrorRevert();
                return FileOpMessage.UnknownError;
            }
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

        public override void LoadErrorRevert()
        {

        }
        public override void SaveErrorRevert()
        {
            PlayerFile.ApplyBackup();
            ProgressFile.ApplyBackup();
            WorldChangesFile.ApplyBackup();
        }

        public override void ExportDisplayData(out object resultF)
        {
            PlayerFile.LoadFromFile(out JObject PlayerData);
            ProgressFile.LoadFromFile(out JObject ProgressData);

            TimeSpan readTime = TimeSpan.Parse((string)ProgressData["playTime"]);
            SaveData.MenuDisplayData result = new()
            {
                timeString = $"{(int)readTime.TotalHours}:{readTime.Minutes:D2}:{readTime.Seconds:D2}",
                location = PlayerData["location"],
                completionPercentage = (float)ProgressData["Completion"],
                health = (int)PlayerData["MaxHealth"],
                powerEggs = (int)ProgressData["powerEggs"]["total"],
                hensRescued = (int)ProgressData["hensRescued"]["total"],
            };
            resultF = result;
        }

        public override void DeleteFile()
        {
            PlayerFile.DeleteFile();
            ProgressFile.DeleteFile();
            WorldChangesFile.DeleteFile();
        }
    }
}
