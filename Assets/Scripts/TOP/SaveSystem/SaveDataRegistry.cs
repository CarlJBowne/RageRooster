using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using RageRooster.Core.Save;
using RageRooster.Core.World;
using RageRooster.Player;
using RageRooster.TOP.Save.Streams;
using SLS.SaveFileCore;
using SLS.Singletons;
using UnityEngine;

namespace RageRooster.TOP.Save
{
    /// <summary>
    /// A Globally acessible registry for all saved values in the game. <br/>
    /// An Asset where defaults are defined and cloned from. <br/>
    /// Also an override of <see cref="SaveDataManager"/> defining its functionality.
    /// </summary>
    public sealed class SaveDataRegistry : SaveDataManager
    {
        [SerializeField] private PlayerStats playerStatsDef = new();
        [SerializeField] private SavedCollectible powerEggs = new();
        [SerializeField] private SavedCollectible wishbones = new();
        [SerializeField] private SavedCollectible hensRescued = new();
        [SerializeField] private Flag.Collection globalFlagDefs = new();
        [SerializeField] private Flag.BoolOnlyCollection storyFlagDefs = new();

        public override void OnInit()
        {
            //Init File background services.
            SaveFile._versionOptions.DesiredFileVersion = typeof(FileVersion_10);
            SaveFile._versionOptions.AllFileVersions = new Dictionary<string, Type>()
            {
                { FileVersion_10.NUM, typeof(FileVersion_10) },
                { FileVersion_UCF.NUM, typeof(FileVersion_UCF) }
            };

            DefsData = new()
            {
                playerStats = playerStatsDef,
                progress = new()
                {
                    storyFlags = storyFlagDefs,
                    powerEggs = powerEggs,
                    wishbones = wishbones,
                    hensRescued = hensRescued,
                },
                flags = AreaRegistry.All.ToDictionary(a => a.name, a => a.flagDefaults)
            };
            DefsData.flags.Add("Global", globalFlagDefs);
        }
        [UnityEngine.SerializeField] private SaveData activeDisplayData = null;

        public static SaveData TransferSnapshot;
        public static SaveFile ActiveFile = null;



        public override void InitializeFile(int fileNo)
        {
            ActiveData = new();
            ActiveData.playerStats.Establish();
            ActiveData.progress.Establish();

            DeathReloadData = new();

#if UNITY_EDITOR
            Debug.Log($"Initializing Save Manager using File {fileNo}");
#endif
            TransferSnapshot = new SaveData();
            ActiveFile = new SaveFile(fileNo);
            RevertToFile();
            activeDisplayData = ActiveData;
        }
        public override void ExitFile()
        {
            ActiveFile = null;
            activeDisplayData = null;
            activeDisplayData = null;
        }
        public override void OnDeload() => ExitFile();

        public override void SaveToFile()
        {
            SaveData.Active.progress.playTime += TimeSpan.FromSeconds(SavedProgress.UpdateGameTime());
            SaveData.Clone(SaveData.Active, SaveData.DeathReloadData);
            SaveData.Clone(SaveData.Active, TransferSnapshot);
            ActiveFile.SaveToFile();
        }
        public override void RevertToFile()
        {
            SavedProgress.UpdateGameTime();
            ActiveFile.LoadFromFile();
            SaveData.Clone(TransferSnapshot, SaveData.Active);
            SaveData.Clone(SaveData.Active, SaveData.DeathReloadData);
        }

        public override void SaveToDeathData() => SaveData.Clone(SaveData.Active, SaveData.DeathReloadData);
        public override void RevertToDeathData() => SaveData.Clone(SaveData.DeathReloadData, SaveData.Active);

    }
}