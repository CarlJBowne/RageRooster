using RageRooster;
using RageRooster.Core.Save;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RageRooster.Actors.Save.Collectibles
{
    public class PowerEgg : CollectibleBase
    {
        protected override SavedCollectible targetSavedCollectible => SaveData.Active.progress.powerEggs;

        private void OnTriggerEnter(Collider other)
        {
            Acquire();
        }

        [CustomEditor(typeof(PowerEgg))]
        public new class Editor : CollectibleBase.Editor
        {
            protected override List<string> targetRegistryList => SaveData.Default.progress.powerEggs.IDs;
        }
    }
}