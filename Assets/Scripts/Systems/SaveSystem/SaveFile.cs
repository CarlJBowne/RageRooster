using Newtonsoft.Json.Linq;
using RageRooster.RoomSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RageRooster.Systems.SaveSystem
{
    public class SaveFile
    {

        public TransitionDestination location;
        public SavedPlayerStats playerStats;
        public PowerEggs powerEggs;
        public SavedHens hensRescued;
        public PermanentChanges globalChanges;
        public Dictionary<AreaAsset, PermanentChanges> areaChanges;

        int fileID = 0;

        public JsonFile playerFile;
        //Contains location and playerStats.

        public JsonFile worldChangesFile;
        //Contains powerEggs, hensRescued, and globalChanges

        public Dictionary<AreaAsset, JsonFile> areaChangesFiles;

        public class SavedPlayerStats
        {
            public int maxHealth;
            public int maxAmmo;
            public int powerEggs;
            public Dictionary<string, bool> upgrades;
        }

        public class PowerEggs
        {
            public int totalEggs;
            public List<bool> isCollected;
        }

        public class SavedHens
        {
            public int totalHens;
            public List<bool> isRescued;
        }

        public class PermanentChanges
        {
            public Dictionary<string, bool> switches;
        }


        public SaveFile(int fileID)
        {
            this.fileID = fileID;
        }


        public JsonFile.LoadResult Load()
        {
            JsonFile.LoadResult result;
            result = playerFile.LoadFromFile();
            if(result != JsonFile.LoadResult.Success) return result;
            result = worldChangesFile.LoadFromFile();
            if (result != JsonFile.LoadResult.Success) return result;
            foreach (var item in areaChangesFiles)
            {
                item.Value.LoadFromFile();
                if (result != JsonFile.LoadResult.Success) return result;
            }

            location = TransitionDestination.Deserialize(playerFile.Data[nameof(location)]);
            JToken playerStatsLoad = playerFile.Data[nameof(playerStats)];
            playerStats.maxHealth = (int)playerStatsLoad[nameof(SavedPlayerStats.maxHealth)];
            playerStats.maxAmmo = (int)playerStatsLoad[nameof(SavedPlayerStats.maxAmmo)];
            playerStats.powerEggs = (int)playerStatsLoad[nameof(SavedPlayerStats.powerEggs)];

            JToken powerEggsLoad = worldChangesFile.Data[nameof(powerEggs)];
            JToken hensRescuedLoad = worldChangesFile.Data[nameof(hensRescued)];
            JToken globalChangesLoad = worldChangesFile.Data[nameof(globalChanges)];

            powerEggs.totalEggs = (int)powerEggsLoad[nameof(PowerEggs.totalEggs)];
            //Continue work


            throw new NotImplementedException();
            return JsonFile.LoadResult.Success;
        }

        public JsonFile.FileState Save()
        {


            throw new NotImplementedException();
            return JsonFile.FileState.Valid;
        }

    }
}
