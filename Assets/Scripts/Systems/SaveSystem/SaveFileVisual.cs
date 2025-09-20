using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RageRooster.Systems.SaveSystem
{
    public class SaveFileVisual : MonoBehaviour
    {
        public int ID = 1;
        public TMPro.TextMeshProUGUI timeText;
        public TMPro.TextMeshProUGUI completionText;
        public TMPro.TextMeshProUGUI totalHealthText;
        public TMPro.TextMeshProUGUI powerEggsText;
        public TMPro.TextMeshProUGUI hensRescuedText;

        SaveFile.IOSystem file;

        private void Awake()
        {
            file = new();
            file.SetFileTarget(ID);
            UpdateFile();
        }


        public void PlayFile() => Gameplay.BeginSaveFile(ID);

        public void DeleteFile()
        {
            file.Delete();
            UpdateFile();
        }

        private void UpdateFile()
        {
            if (file.doesFileExist)
            {
                file.Load();

                timeText.enabled = true;
                completionText.enabled = true;
                totalHealthText.enabled = true;
                powerEggsText.enabled = true;
                hensRescuedText.enabled = true;


                var TS = (TimeSpan)file.playerFile.Data[nameof(SaveFile.SavedPlayerStats.playTime)];
                timeText.text = $"{TS.Hours}:{TS.Minutes}:{TS.Seconds}";
                completionText.text = $"{file.GetCompletionPercentage()}%";
                totalHealthText.text = file.playerFile.Data[nameof(SaveFile.SavedPlayerStats.maxHealth)].ToString();
                powerEggsText.text = file.worldChangesFile.Data[nameof(SaveFile.powerEggs)][nameof(SaveFile.SavedCollectible.total)].ToString();
                hensRescuedText.text = file.playerFile.Data[nameof(SaveFile.hensRescued)][nameof(SaveFile.SavedCollectible.total)].ToString();
            }
            else
            {
                timeText.enabled = false;
                completionText.enabled = false;
                totalHealthText.enabled = false;
                powerEggsText.enabled = false;
                hensRescuedText.enabled = false;
            }

                

        }
    }
}