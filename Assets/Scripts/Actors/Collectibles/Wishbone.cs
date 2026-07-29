using RageRooster.SaveSystem;
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
    public class Wishbone : CollectibleBase
    {
        protected override List<string> targetRegistryList => SavedValueRegistry.Wishbones;

        protected override SaveData.SavedCollectible targetSavedCollectible => SaveData.Current.wishbones;


        private void OnTriggerEnter(Collider other)
        {
            Acquire();
        }

        [CustomEditor(typeof(Wishbone))]
        public new class Editor : CollectibleBase.Editor { }
    }
}