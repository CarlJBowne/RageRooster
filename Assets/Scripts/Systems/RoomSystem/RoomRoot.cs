using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Linq;

namespace RageRooster.RoomSystem
{
    /// <summary>
    /// The Root component for an Area. Attached to the root <see cref="GameObject"/> of a <see cref="RoomAsset.scene"/>
    /// <br/> If a Room is created via the File/CreateRoom button, this component is automatically setup.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrders.Room)]
    public class RoomRoot : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="RoomAsset"/> associated with this instance.
        /// </summary>
        [field: SerializeField] public RoomAsset asset { get; protected set; }
        /// <summary>
        /// The defined <see cref="SpawnPoint"/>s available in this room.
        /// <br/> Automatically populated upon saving the scene in the editor.
        /// </summary>
        [field: SerializeField] public SpawnPoint[] spawns { get; protected set; }

        private void Awake()
        {
            if (!RoomManager.Active)
            {
                if (EditorState.EditorDestination.IsNull())
                    EditorState.EditorDestination = new()
                    {
                        room = asset,
                        spawnID = -1
                    };
                Gameplay.BeginEditor();
                return;
            }

            asset.Connect(this);
        }



#if UNITY_EDITOR



        public class Editor : UnityEditor.Editor
        {

            public static void AttachAsset(RoomRoot This, RoomAsset room)
            {
                This.asset = room;
                UnityEditor.EditorUtility.SetDirty(This);
            }

            [InitializeOnLoad]
            public static class RoomRootSceneHook
            {
                static RoomRootSceneHook() => UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;

                private static void OnSceneSaving(Scene scene, string path)
                {
                    if (!scene.GetRootGameObjects()[0].TryGetComponent(out RoomRoot roomRoot)) return;
                    OnSaveScene(scene, roomRoot);
                }
            }

            public static void OnSaveScene(Scene scene, RoomRoot root)
            {
                if (root.asset == null)
                {
                    throw new System.Exception($"ERROR: The RoomRoot in scene {scene.name} does not have an associated RoomAsset. Please create a RoomAsset and assign it to the RoomRoot before saving the scene.");
                }

                List<IRoomObject> roomObjects = root.gameObject.GetComponentsInChildren<IRoomObject>().ToList();

                root.asset.entrances.Clear();
                int spawnPointID = 0;
                List<SpawnPoint> spawns = new();

                for (int i = 0; i < roomObjects.Count; i++)
                {
                    if (roomObjects[i] is RoomEntrance) roomObjects[i].OnSaveScene(root);
                    if (roomObjects[i] is SpawnPoint spawnPoint)
                    {
                        roomObjects[i].OnSaveScene(root, spawnPointID);
                        spawnPoint.ID = spawnPointID++;
                        spawns.Add(spawnPoint);
                        EditorUtility.SetDirty(spawnPoint);
                    }
                }

                root.spawns = spawns.ToArray();


                EditorUtility.SetDirty(root.asset);
                EditorUtility.SetDirty(root);
            }
        }

#endif
    }
    public interface IRoomObject
    {
        RoomRoot root { get; set; }
        public RoomRoot GetRoot() => root;
        protected static void ConnectToRoomRoot(MonoBehaviour obj)
        {
            if (obj is not IRoomObject roomObj || roomObj.root != null) return;
            RoomRoot foundRoot = obj.GetComponentInParent<RoomRoot>();
            if (foundRoot == null)
                throw new System.Exception($"The object {obj.name} is not inside a RoomRoot and cannot connect to it.");
            roomObj.root = foundRoot;
        }
        internal virtual object OnSaveScene(RoomRoot room, params object[] args) => null;
    }
}