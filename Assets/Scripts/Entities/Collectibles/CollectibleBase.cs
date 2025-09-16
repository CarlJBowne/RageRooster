

using RageRooster.Systems.SaveSystem;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace RageRooster.Entities.Collectibles
{
    
    public abstract class CollectibleBase : MonoBehaviour
    {

        [SerializeField, HideInInspector] protected string ID;

        protected abstract List<string> targetRegistryList { get; }
        protected abstract SaveFile.SavedCollectible targetSavedCollectible { get; }


        protected virtual void Awake()
        {
            if (targetSavedCollectible.isCollected[targetRegistryList.IndexOf(ID)])
                gameObject.SetActive(false);
        }

        protected virtual void Acquire()
        {
            targetSavedCollectible.isCollected[targetRegistryList.IndexOf(ID)] = true;
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR

        protected void SetID(string input)
        {
            if (!string.IsNullOrEmpty(ID) && targetRegistryList.Contains(ID))
                targetRegistryList[targetRegistryList.IndexOf(ID)] = input;
            else
                targetRegistryList.Add(input);
            ID = input;
            EditorUtility.SetDirty(SavedValueManager.Get());
            EditorUtility.SetDirty(this);
        }

        protected void DELETE()
        {
            if (!EditorUtility.DisplayDialog("Delete Collectible",
                "Are you sure you want to delete this Collectible? This action cannot be undone, " +
                "it will remove the Game Object, automatically save this scene, and remove the Collectible from the registry.",
                "Delete", "Cancel")) return;

            targetRegistryList.Remove(ID);
            DestroyImmediate(gameObject);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                CollectibleBase This = target as CollectibleBase;

                if (string.IsNullOrEmpty(This.ID))
                {
                    if (GUILayout.Button("Generate Global ID"))
                    {
                        RoomSystem.RoomRoot room = This.transform.FindComponentInAncestry<RoomSystem.RoomRoot>();
                        if (room == null) throw new System.Exception("Wishbone must be a child of a RoomRoot to generate an ID.");
                        This.SetID($"{room.asset.area.name}_{room.asset.name}_{System.Guid.NewGuid()}");
                    }
                }
                else
                {
                    string newID = EditorGUILayout.TextField("ID", This.ID);
                    if (newID != This.ID)
                        This.SetID(newID);
                    if (GUILayout.Button("Delete")) This.DELETE();
                }
                
            }
        }
#endif
    }
}