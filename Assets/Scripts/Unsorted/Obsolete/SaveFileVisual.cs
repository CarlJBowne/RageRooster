using Newtonsoft.Json.Linq;
using RageRooster.Core.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities.JSON;

namespace RageRooster.Obsolete
{
    [System.Obsolete]
    public class SaveFileVisual : MonoBehaviour
    {
        public int ID = 1;
        public TMPro.TextMeshProUGUI timeText;
        private JsonFile File;

        private void Awake()
        {
            //File = new(GlobalState.SaveFilePath, $"SaveFile{ID}");
            UpdateFile();
        }


        public void PlayFile() => Gameplay.BeginSaveFile(ID);

        public void DeleteFile()
        {
            //GlobalState.DeleteSaveFile(ID);
            UpdateFile();
        }

        private void UpdateFile()
        {

            if (File.LoadFromFile() == JsonFile.LoadResult.Success)
            {
                var TS = System.TimeSpan.FromSeconds(File.Data["Time"].ToObject<double>());
                timeText.text = $"{TS.Hours}:{TS.Minutes}:{TS.Seconds}";
            }
            else { timeText.text = "Empty"; }
        }
    }
}