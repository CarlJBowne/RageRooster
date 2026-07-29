using RageRooster;
using RageRooster.SaveSystem;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RageRooster.Entities.Collectibles
{
    public class PowerEgg : CollectibleBase
    {
        protected override List<string> targetRegistryList => SavedValueRegistry.PowerEggs;

        protected override SaveData.SavedCollectible targetSavedCollectible => SaveData.Current.powerEggs;


        private void OnTriggerEnter(Collider other)
        {
            Acquire();
        }

        [CustomEditor(typeof(PowerEgg))]
        public new class Editor : CollectibleBase.Editor { }
    }
}