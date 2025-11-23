using RageRooster.Systems.SaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

namespace RageRooster.Entities.Collectibles
{
    public class Hen : CollectibleBase
    {
        protected override List<string> targetRegistryList => SavedValueRegistry.HensRescued;

        protected override SaveData.SavedCollectible targetSavedCollectible => SaveData.Current.hensRescued;


        private void OnTriggerEnter(Collider other)
        {
            Acquire();
        }

        [CustomEditor(typeof(Hen))]
        public new class Editor : CollectibleBase.Editor { }
    }
}