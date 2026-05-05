using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageRooster.Obsolete;
using RageRooster.Systems.SaveSystem;
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


        protected override List<string> targetRegistryList => SavedValueRegistry.HensRescued;

        protected override SaveData.SavedCollectible targetSavedCollectible => SaveData.Current.hensRescued;

        private void OnTriggerEnter(Collider other)
        {
            Player.Ammo.Max++;
            Acquire();
            UIHUDSystem.Instance.ShowHint(hintString);
        }

        [CustomEditor(typeof(Hen))]
        public new class Editor : CollectibleBase.Editor { }
    }
}