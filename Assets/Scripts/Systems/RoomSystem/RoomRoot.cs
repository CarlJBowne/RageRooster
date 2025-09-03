using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace RageRooster.RoomSystem
{
    [DefaultExecutionOrder(-200)]
    public partial class RoomRoot : MonoBehaviour
    {
        [field: SerializeField] public RoomAsset asset { get; protected set; }
        [field: SerializeField] public SpawnPoint[] spawns { get; protected set; }

        private void Awake()
        {
            if (!RoomManager.Active)
            {
                if (!EditorState.EditorDestination.IsValid()) EditorState.EditorDestination = new(this);
                Gameplay.BeginEditor(EditorState.EditorDestination);
                return;
            }

            asset.Connect(this);
        }


    }

#if UNITY_EDITOR
    public partial class RoomRoot
    {
        internal void OnSaveScene(Scene scene)
        {
            if (asset == null)
            {
                throw new System.Exception($"ERROR: The RoomRoot in scene {scene.name} does not have an associated RoomAsset. Please create a RoomAsset and assign it to the RoomRoot before saving the scene.");
            }

            RoomTransition[] transitions = gameObject.GetComponentsInChildren<RoomTransition>();

            asset.transitions.Clear();
            foreach (RoomTransition transition in transitions)
            {
                transition.root = this;
                asset.transitions.Add(transition.GetData());
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
    }

    [InitializeOnLoad]
    public static class _RoomRootSceneHook
    {
        static _RoomRootSceneHook() => UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (!scene.GetRootGameObjects()[0].TryGetComponent(out RoomRoot roomRoot)) return;
            roomRoot.OnSaveScene(scene);
        }
    }
#endif
}