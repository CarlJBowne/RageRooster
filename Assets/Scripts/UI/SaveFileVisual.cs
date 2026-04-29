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

        SaveData data;
        SaveData.IOStream file;

        private void Awake()
        {
            file = new(ID);
            UpdateFile();
        }


        public void PlayFile() => Gameplay.BeginSaveFile(ID);

        public void DeleteFile()
        {
            file.DeleteFile();
            UpdateFile();
        }

        private void UpdateFile()
        {
            if (file.filesDoExist)
            {
                file.LoadFromFile(data);

                details.SetActive(true);

                Destination location = data.location;
                locationText.text = $"{location.area.displayName} -- {location.room.displayName}";

                var TS = data.playerStats.playTime;
                timeText.text = $"{TS.Hours}:{TS.Minutes}:{TS.Seconds}";

                completionText.text = $"{file.GetCompletionPercentage()}%";

                totalHealthText.text = data.playerStats.maxHealth.ToString();
                powerEggsText.text = data.powerEggs.total.ToString();
                hensRescuedText.text = data.hensRescued.total.ToString();
            }
            else
            {
                details.SetActive(false);
            }



        }
    }
}