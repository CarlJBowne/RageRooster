using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace RageRooster.RoomSystem
{
    [DefaultExecutionOrder(ExecutionOrders.Room)]
    public class RoomRoot : MonoBehaviour
    {
        [field: SerializeField] public RoomAsset asset { get; protected set; }
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

        internal void OnSaveScene(Scene scene)
        {
            if (asset == null)
            {
                throw new System.Exception($"ERROR: The RoomRoot in scene {scene.name} does not have an associated RoomAsset. Please create a RoomAsset and assign it to the RoomRoot before saving the scene.");
            }

            RoomEntrance[] transitions = gameObject.GetComponentsInChildren<RoomEntrance>();

            asset.entrances.Clear();
            foreach (RoomEntrance transition in transitions)
            {
                transition.root = this;
                asset.entrances.Add(transition.GetData());
            }

            spawns = gameObject.GetComponentsInChildren<SpawnPoint>();
            for (int i = 0; i < spawns.Length; i++)
            {
                spawns[i].root = this;
                spawns[i].ID = i;
                EditorUtility.SetDirty(spawns[i]);
            }

            EditorUtility.SetDirty(asset);
            EditorUtility.SetDirty(this);
        }

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
                    roomRoot.OnSaveScene(scene);
                }
            }

        }

#endif
    }

}