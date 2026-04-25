using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AYellowpaper;
 
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

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
        [field: SerializeField] public List<GameObject> RootGameObjects { get; private set; } = new();

        [field: SerializeField] public IComponentList<IRoomActor> RoomActors { get; private set; } = new();

        [field: SerializeField] public List<SpawnPoint> Spawns { get; internal set; }
        [field: SerializeField] public List<RoomEntrance> EntranceActors { get; internal set; }

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

        public static RoomRoot Find(Scene scene)
        {
            if (scene == null) return null;

            if (scene.GetRootGameObjects()[0].TryGetComponent(out RoomRoot firstAttempt)) return firstAttempt;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                RoomRoot room = root.GetComponentInChildren<RoomRoot>(true);
                if (room != null)
                    return room;
            }

            return null;
        }
        public static RoomRoot Find(GameObject G) => Find(G.scene);
        public static RoomRoot Find(Component C) => Find(C.gameObject.scene);

        public List<T> FindComponentsInRoom<T>()
        {
            List<T> result = new();
            for (int i = 0; i < RootGameObjects.Count; i++)
                result.AddRange(RootGameObjects[i].GetComponentsInChildren<T>());
            return result;
        }

#if UNITY_EDITOR
        [ContextMenu("Force Registration")]
        private void ForceRegistration()
        {
            RootGameObjects = gameObject.scene.GetRootGameObjects().ToList();

            RoomActors.Clear();
            var actors = FindComponentsInRoom<IRoomActor>();
            Spawns.Clear();
            EntranceActors.Clear();
            asset.entrances.Clear();
            asset.spawnPointNames.Clear();
            foreach (IRoomActor actor in actors) IRoomActor.RegisterWithRoot(actor, true);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(asset);
        }

        [CustomEditor(typeof(RoomRoot))]
        public class Editor : UnityEditor.Editor
        {
            RoomRoot This;
            public override VisualElement CreateInspectorGUI()
            {
                VisualElement root = new();

                SerializedProperty scriptProp = serializedObject.FindProperty("m_Script");
                PropertyField scriptField = new(scriptProp);
                scriptField.SetEnabled(false);
                root.Add(scriptField);


                This = target as RoomRoot;

                PropertyField assetField = new(serializedObject.FindProperty(nameof(asset).BackingField()));
                root.Add(assetField);

                Foldout MakeDisplayOnlyList(string propName, string displayName)
                {
                    SerializedProperty prop = serializedObject.FindProperty(propName);
                    Foldout foldout = new()
                    {
                        text = $"{displayName} : {prop.arraySize}",
                        value = false
                    };
                    for (int i = 0; i < prop.arraySize; i++)
                    {
                        var iProp = prop.GetArrayElementAtIndex(i);
                        PropertyField iPropField = new(iProp, "");
                        foldout.Add(iPropField);
                        iPropField.SetEnabled(false);
                    }
                    return foldout;
                }

                root.Add(MakeDisplayOnlyList(nameof(RootGameObjects).BackingField(), "Root GameObjects"));
                root.Add(MakeDisplayOnlyList($"{nameof(RoomActors).BackingField()}.list", "All RoomActors"));
                root.Add(MakeDisplayOnlyList(nameof(EntranceActors).BackingField(), "Entrances"));

                return root;
            }


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
                    GameObject[] sceneGameObjects = scene.GetRootGameObjects();

                    if (!scene.GetRootGameObjects()[0].TryGetComponent(out RoomRoot root)) return;

                    root.RootGameObjects = sceneGameObjects.ToList();

                    if (root.asset == null)
                    {
                        throw new System.Exception($"ERROR: The RoomRoot in scene {scene.name} does not have an associated RoomAsset. Please create a RoomAsset and assign it to the RoomRoot before saving the scene.");
                    }

                    foreach (var actor in root.FindComponentsInRoom<IRoomActor>())
                    {
                        if (actor.Root == null) IRoomActor.RegisterWithRoot(actor, true);
                        actor.OnSave();
                        EditorUtility.SetDirty(root);
                        EditorUtility.SetDirty(root.asset);
                    }

                    /*
                    var types = typeof(IRoomActor).GetAllChildTypes(true);

                    foreach (var type in types)
                    {
                        var objsOfType = roomObjects.Where(o => o.GetType() == type).ToList();
                        if (objsOfType.Count == 0) continue;

                        MethodInfo targetMethod = type.GetMethod("OnSaveSceneSet", BindingFlags.NonPublic | BindingFlags.Static);
                        if (targetMethod != null)
                        {
                            // Use LINQ Cast<T>() + ToList<T>() via reflection to produce List<type>
                            object castedEnumerable = typeof(Enumerable)
                                .GetMethod(nameof(Enumerable.Cast), BindingFlags.Public | BindingFlags.Static)
                                .MakeGenericMethod(type)
                                .Invoke(null, new object[] { objsOfType });

                            // casts objsOfType (IEnumerable) to IEnumerable<type>, then ToList<type>()
                            object typedListInstance =
                                typeof(Enumerable)
                                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                                .First(m => m.Name == nameof(Enumerable.ToList) && m.GetParameters().Length == 1)
                                .MakeGenericMethod(type)
                                .Invoke(null, new object[] { castedEnumerable });

                            targetMethod.Invoke(null, new object[] { root, typedListInstance });
                            continue;
                        }

                        targetMethod = type.GetMethod("OnSaveScene", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (targetMethod != null)
                        {
                            foreach (var item in objsOfType) targetMethod.Invoke(item, new object[] { root });
                            continue;
                        }

                    }
                    */
                    EditorUtility.SetDirty(root.asset);
                    EditorUtility.SetDirty(root);
                }
            }
        }

#endif
    }
}