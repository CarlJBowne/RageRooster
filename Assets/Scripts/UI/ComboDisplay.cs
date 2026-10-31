using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace RageRooster.UI.HUD
{
    [System.Obsolete("This has NOT been implemented yet.")]
    public class ComboDisplay : MonoBehaviour
    {
        public Timer.OneTime comboTime;
        public TextMeshProUGUI comboCounterText;
        public TextMeshProUGUI comboFlavorText;
        public ComboLevel[] comboLevels;

        int currentCombo;

        // Adds to the combo count
        //public static void AddCombo() => Instance.combo.AddCombo_();
        private void AddCombo_()
        {
            currentCombo++;
            comboTime.Begin();
            comboCounterText.enabled = true;
            comboCounterText.text = currentCombo.ToString();
            if (currentCombo >= comboLevels[0].req)
            {
                int i = 0; //Cooler solution.
                for (; i < comboLevels.Length && currentCombo >= comboLevels[i + 1].req; i++) ;
                //int F = 0; //More sure solution.
                //for (int i = 1; i < comboLevels.Length && currentCombo >= comboLevels[i].req; i++) F = i;
                comboFlavorText.enabled = true;
                comboFlavorText.text = comboLevels[i].flavorText;
            }
        }

        // Ends the current combo
        public void EndCombo()
        {
            currentCombo = 0;
            comboTime.running = false;
            comboCounterText.enabled = false;
            comboFlavorText.enabled = false;
        }

        [Serializable]
        public struct ComboLevel
        {
            // Required combo count to reach this level
            public int req;
            // Flavor text for this combo level
            public string flavorText;
        }

    }
}
