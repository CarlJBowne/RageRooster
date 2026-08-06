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
        protected override SavedCollectible targetSavedCollectible => SavedCollectible.Wishbones;


        private void OnTriggerEnter(Collider other)
        {
            Acquire();
        }

        [CustomEditor(typeof(Wishbone))]
        public new class Editor : CollectibleBase.Editor
        {
            protected override List<string> targetRegistryList => SavedValueRegistry.Wishbones;
        }
    }
}