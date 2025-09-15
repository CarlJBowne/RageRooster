using RageRooster.RoomSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.iOS;
using static RageRooster.Systems.SaveSystem.SaveFile;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RageRooster.Entities.Collectibles
{
    public class PowerEgg : MonoBehaviour
    {
        [SerializeField] private string ID;

        private void Reset() => GenerateGlobalID();

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (SavedValueManager.PowerEggs.Contains(ID))
                {
                    SavedValueManager.PowerEggs.Remove(ID);
                }
            }
#endif
        }

        private void GenerateGlobalID()
        {
            RoomRoot room = transform.FindComponentInAncestry<RoomRoot>();
            if (room == null) throw new System.Exception("PowerEgg must be a child of a RoomRoot to generate an ID.");
            while(string.IsNullOrEmpty(ID) || SavedValueManager.PowerEggs.Contains(ID))
                ID = $"{room.asset.area.name}_{room.asset.name}_{System.Guid.NewGuid()}";
            AddToRegistry();
        }

        private void AddToRegistry() => SavedValueManager.PowerEggs.Add(ID);


#if UNITY_EDITOR
        [CustomEditor(typeof(PowerEgg))]
        public class PowerEggEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                PowerEgg powerEgg = target as PowerEgg;

                if (string.IsNullOrEmpty(powerEgg.ID))
                { if (GUILayout.Button("Generate Global ID")) powerEgg.GenerateGlobalID(); }
                else
                {
                    string newID = EditorGUILayout.TextField("ID", powerEgg.ID);
                    if (newID != powerEgg.ID && !SavedValueManager.PowerEggs.Contains(newID))
                    {
                        SavedValueManager.PowerEggs[SavedValueManager.PowerEggs.IndexOf(powerEgg.ID)] = newID;
                        powerEgg.ID = newID;
                    }
                    if (!SavedValueManager.PowerEggs.Contains(powerEgg.ID) && GUILayout.Button("Add to Registry")) powerEgg.AddToRegistry();
                }
            }
        }
#endif
    }
}