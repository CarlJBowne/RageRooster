

using RageRooster.Systems.SaveSystem;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using RageRooster.RoomSystem;

namespace RageRooster.Entities.Collectibles
{
    /// <summary>
    /// A base class for all collectible items in the game. Interfaces with <see cref="SaveData.SavedCollectible"/> and <see cref="SavedValueRegistry"/> to manage collectible state.
    /// </summary>
    public abstract class CollectibleBase : MonoBehaviour
    {
        /// <summary>
        /// The unique identifier for this collectible, saved in the <see cref="SavedValueRegistry"/> <br/>
        /// Should be globally unique, generally not managed manually. <br/>
        /// Generated IDs are in the format: {AreaName}_{RoomName}_{GUID}
        /// </summary>
        [SerializeField, HideInInspector] protected string ID;

        protected abstract List<string> targetRegistryList { get; }
        protected abstract SaveData.SavedCollectible targetSavedCollectible { get; }


        protected virtual void Awake()
        {
            if (targetSavedCollectible.isCollected[targetRegistryList.IndexOf(ID)])
                gameObject.SetActive(false);
        }

        /// <summary>
        /// The Method to call when the player acquires this collectible.
        /// </summary>
        protected virtual void Acquire()
        {
            targetSavedCollectible.isCollected[targetRegistryList.IndexOf(ID)] = true;
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR

        /// <summary>
        /// Sets the unique ID for this collectible into the <see cref="SavedValueRegistry"/> registry. <br/>
        /// </summary>
        /// <param name="input">The new ID to set.</param>
        protected void SetID(string input)
        {
            if (!string.IsNullOrEmpty(ID) && targetRegistryList.Contains(ID))
                targetRegistryList[targetRegistryList.IndexOf(ID)] = input;
            else
                targetRegistryList.Add(input);
            ID = input;
            EditorUtility.SetDirty(SavedValueRegistry.Get());
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Deletes this collectible from the registry and destroys the GameObject.
        /// </summary>
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
                        RoomRoot room = RoomRoot.Find(This);
                        if (room == null) throw new System.Exception("Wishbone must be in a properly configured Room scene to generate an ID.");
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