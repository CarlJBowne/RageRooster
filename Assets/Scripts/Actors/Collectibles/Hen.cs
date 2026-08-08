using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageRooster.Player;
using RageRooster.Core.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static RageRooster.Services;

namespace RageRooster.Actors.Save.Collectibles
{
    public class Hen : CollectibleBase
    {
        public string henName = "INSERT_HEN_NAME_HERE";
        public int ammoCount = 1;
        public string hintString;

        protected override SavedCollectible targetSavedCollectible => SaveData.Active.progress.hensRescued;

        private void OnTriggerEnter(Collider other)
        {
            PlayerStats.Active.MaxAmmo++;
            Acquire();
            UI.ShowHint(hintString);
        }

        [CustomEditor(typeof(Hen))]
        new public class Editor : CollectibleBase.Editor
        {
            protected override List<string> targetRegistryList => SaveData.Default.progress.hensRescued.IDs;
        }
    }
}