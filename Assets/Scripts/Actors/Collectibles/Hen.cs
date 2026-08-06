using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageRooster.Obsolete;
using RageRooster.SaveSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RageRooster.Entities.Collectibles
{
    public class Hen : CollectibleBase
    {
        public string henName = "INSERT_HEN_NAME_HERE";
        public int ammoCount = 1;
        public string hintString;

        protected override SavedCollectible targetSavedCollectible => SavedCollectible.Hens;

        private void OnTriggerEnter(Collider other)
        {
            Player.Ammo.Max++;
            Acquire();
            UIHUDSystem.Instance.ShowHint(hintString);
        }

        [CustomEditor(typeof(Hen))]
        public class Editor : CollectibleBase.Editor
        {
            protected override List<string> targetRegistryList => SavedValueRegistry.HensRescued;
        }
    }
}