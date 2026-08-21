using RageRooster.World;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RageRooster.Core.Save;


namespace RageRooster.TOP.Save
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
        TOP.Save.SaveFile file;

        private void Awake()
        {
            file = new(ID);
            UpdateFile();
        }


        public void PlayFile() => Gameplay.BeginSaveFile(ID);

        public void DeleteFile()
        {
            file.Stream.DeleteFile();
            UpdateFile();
        }

        private void UpdateFile()
        {
            file.ExportMenuDisplayData(out SaveData.MenuDisplayData data);
            if (data.isValid)
            {
                details.SetActive(true);

                locationText.text = $"{data.location.area} -- {data.location.room}";

                var TS = data.timeString;
                timeText.text = TS;
                //timeText.text = $"{TS.Hours}:{TS.Minutes}:{TS.Seconds}";

                completionText.text = $"{data.completionPercentage * 100}%";

                totalHealthText.text = data.health.ToString();
                powerEggsText.text = data.powerEggs.ToString();
                hensRescuedText.text = data.hensRescued.ToString();
            }
            else
            {
                details.SetActive(false);
            }



        }
    }
}