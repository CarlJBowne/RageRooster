using System;
using System.Collections.Generic;
using System.Text;
using RageRooster.Core.Save;
using RageRooster.TOP.Save.Streams;
using Unity.VisualScripting;
using UnityEngine;

namespace RageRooster.TOP.Save
{
    /// <summary>
    /// This class manages active in-gameplay functionality for Save Data Management. <br/>
    /// Contains 1 active <see cref="SaveFile"/> to manage the active connection to the relevant numbered Save File. <br/>
    /// Saving when the active <see cref="SaveFile"/> is not using an IO stream of the most recent version will result in the <see cref="SaveFile"/> and its counterpart data on disk being flushed and replaced with the most recent version.
    /// </summary>
    public static class SaveManager
    {
        public static SaveData TransferSnapshot;
        public static SaveFile Active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            SaveData.SaveToSaveFile = SaveToSaveFile;
            SaveData.RevertToSaveFile = RevertToSaveFile;
            SaveData.CallInitializeSave = InitializeManager;
        }
        public static void InitializeManager(int fileNo)
        {
            TransferSnapshot = new SaveData();
            Active = new SaveFile(fileNo);
            SaveData.InitializeSystem();
            RevertToSaveFile();
        }

        public static void SaveToSaveFile()
        {
            SaveData.Active.progress.playTime += TimeSpan.FromSeconds(SavedProgress.UpdateGameTime());
            SaveData.Clone(SaveData.Active, SaveData.DeathReloadData);
            SaveData.Clone(SaveData.Active, TransferSnapshot);
            Active.SaveToFile();
        }
        public static void RevertToSaveFile()
        {
            SavedProgress.UpdateGameTime();
            Active.LoadFromFile();
            SaveData.Clone(TransferSnapshot, SaveData.Active);
            SaveData.Clone(SaveData.Active, SaveData.DeathReloadData);
        }
    }
}
