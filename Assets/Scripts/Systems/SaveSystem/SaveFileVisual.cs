using RageRooster.RoomSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RageRooster.Systems.SaveSystem
{
    public class SaveFileVisual : MonoBehaviour
    {
        public int ID = 1;
        public GameObject details;
        public TMPro.TextMeshProUGUI locationText;
        public TMPro.TextMeshProUGUI timeText;
        public TMPro.TextMeshProUGUI completionText;
        public TMPro.TextMeshProUGUI totalHealthText;
        public TMPro.TextMeshProUGUI powerEggsText;
        public TMPro.TextMeshProUGUI hensRescuedText;

        SaveData.IOStream file;

        private void Awake()
        {
            file = new(ID);
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

                details.SetActive(true);

                Destination location = file.file.location;
                locationText.text = $"{location.area.displayName} -- {location.room.displayName}";

                var TS = file.file.playerStats.playTime;
                timeText.text = $"{TS.Hours}:{TS.Minutes}:{TS.Seconds}";

                completionText.text = $"{file.GetCompletionPercentage()}%";

                totalHealthText.text = file.file.playerStats.maxHealth.ToString();
                powerEggsText.text = file.file.powerEggs.total.ToString();
                hensRescuedText.text = file.file.hensRescued.total.ToString(); 
            }
            else
            {
                details.SetActive(false);
            }

                

        }
    }
}