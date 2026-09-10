using System;
using System.Collections.Generic;
using System.Text;
using RageRooster.Core.Save;
using RageRooster.TOP.Save.Streams;
using Utilities.JSON;

namespace RageRooster.TOP.Save
{
    /// <summary>
    /// This represents an individual SaveFile ID of indeterminant Stream Version. It will attempt to load the most recent version of the SaveFile, and if it fails, it will attempt to load the next most recent version, and so on until it either finds a valid SaveFile or determines that no SaveFile exists for this ID.
    /// </summary>
    public class SaveFile
    {
        public SaveIOStream Stream;
        private readonly int targetFileID;
        static Type DesiredStreamVersion = typeof(SaveStream150);
        static SaveIOStream DesiredStreamCreator(int fileID, out JsonFile.FileState state) =>
            new SaveStream150(fileID, out state);

        public SaveFile(int fileID)
        {
            targetFileID = fileID;
            Stream = DesiredStreamCreator(fileID, out JsonFile.FileState state);
            if (state != JsonFile.FileState.Valid)
            {
                var firstStream = Stream;
                Stream = new SaveStream100(fileID, out state);
                if (state != JsonFile.FileState.Valid) Stream = firstStream;
            }
        }

        public void LoadFromFile()
        {
            if (Stream.State != JsonFile.FileState.Valid) return;
            SaveData.Clone(SaveData.Default, SaveManager.TransferSnapshot);
            Stream.LoadFromFile();
        }
        public void SaveToFile()
        {
            if (Stream.GetType() != DesiredStreamVersion)
            {
                Stream.DeleteFile();
                Stream = DesiredStreamCreator(targetFileID, out _);
            }
            Stream.SaveToFile();
        }

        public void ExportMenuDisplayData(out SaveData.MenuDisplayData result)
        {
            if (Stream != null) Stream.ExportMenuDisplayData(out result);
            else result = new SaveData.MenuDisplayData { isValid = false };
        }
    }
}
