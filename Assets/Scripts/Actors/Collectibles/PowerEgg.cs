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

        protected override SavedCollectible targetSavedCollectible => SavedCollectible.PowerEggs;

        private void OnTriggerEnter(Collider other)
        {
            Acquire();
        }

        [CustomEditor(typeof(PowerEgg))]
        public new class Editor : CollectibleBase.Editor
        {
            protected override List<string> targetRegistryList => SavedValueRegistry.PowerEggs;
        }
    }
}