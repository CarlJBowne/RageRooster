

using RageRooster.SaveSystem;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using RageRooster.World;

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

        protected abstract SavedCollectible targetSavedCollectible { get; }


        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(ID))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"This {this.GetType().Name} Collectible ({gameObject.name}) is not regsitered. It will work for the time being, but it will disable itself in the final build and in testing will not permanently disappear once collected.");
                return;
#else
            gameObject.SetActive(false);
            return;
#endif
            }

            if (targetSavedCollectible.GetValue(ID))
                gameObject.SetActive(false);
        }

        /// <summary>
        /// The Method to call when the player acquires this collectible.
        /// </summary>
        protected virtual void Acquire()
        {
            targetSavedCollectible.SetValue(ID, true);
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR

        public abstract class Editor : UnityEditor.Editor
        {
            CollectibleBase This;
            string ID
            {
                get => This.ID;
                set => This.ID = value;
            }
            protected abstract List<string> targetRegistryList { get; }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                This = target as CollectibleBase;

                if (string.IsNullOrEmpty(This.ID))
                {
                    if (GUILayout.Button("Generate Global ID"))
                    {
                        RoomRoot room = RoomRoot.Find(This);
                        if (room == null) throw new System.Exception("Wishbone must be in a properly configured Room scene to generate an ID.");
                        SetID($"{room.asset.area.name}_{room.asset.name}_{System.Guid.NewGuid()}");
                    }
                }
                else
                {
                    string newID = EditorGUILayout.TextField("ID", This.ID);
                    if (newID != This.ID)
                        SetID(newID);
                    if (GUILayout.Button("Delete")) DELETE();
                }
                
            }

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
                EditorUtility.SetDirty(SavedValueRegistry.Get);
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
                DestroyImmediate(This.gameObject);

                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }

        }
#endif
    }
}