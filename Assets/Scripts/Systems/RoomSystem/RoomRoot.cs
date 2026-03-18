using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AYellowpaper;
using SLS.ISingleton;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        //[field: SerializeField] public List<Component> roomActors { get; private set; } = new();
        /// <summary>
        /// The defined <see cref="SpawnPoint"/>s available in this room.
        /// <br/> Automatically populated upon saving the scene in the editor.
        /// </summary>
        [field: SerializeField] public SpawnPoint[] spawns { get; internal set; }

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
                    GameObject[] sceneGameObjects = scene.GetRootGameObjects();

                    if (!scene.GetRootGameObjects()[0].TryGetComponent(out RoomRoot root)) return;

                    root.RootGameObjects = sceneGameObjects.ToList();


                    if (root.asset == null)
                    {
                        throw new System.Exception($"ERROR: The RoomRoot in scene {scene.name} does not have an associated RoomAsset. Please create a RoomAsset and assign it to the RoomRoot before saving the scene.");
                    }

                    List<IRoomActor> roomObjects = root.gameObject.GetComponentsInChildren<IRoomActor>().ToList();

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

                    EditorUtility.SetDirty(root.asset);
                    EditorUtility.SetDirty(root);
                }
            }
        }

#endif
    }
    /// <summary>
    /// An interface representing objects with an important connection to the <see cref="RoomRoot"/>/<see cref="RoomAsset"/> they belong to.
    /// <br/> Add the OnSaveSceneSet method to override what happens when the Room's scene is saved in editor.
    /// <br/> (See script for example.)
    /// </summary>
    public interface IRoomActor
    {
        RoomRoot root { get; set; }
        public RoomRoot GetRoot() => root;
        protected static void ConnectToRoomRoot(MonoBehaviour obj)
        {
            if (obj is not IRoomActor roomObj || roomObj.root != null) return;
            RoomRoot foundRoot = obj.GetComponentInParent<RoomRoot>();
            if (foundRoot == null)
                throw new System.Exception($"The object {obj.name} is not inside a RoomRoot and cannot connect to it.");
            roomObj.root = foundRoot;
        }

        //private void OnSaveScene(RoomRoot root)
        //private static void OnSaveSceneSet(RoomRoot root, List<CLASSTYPE> list)
    }

    public static class _RoomActorExtensions
    {
        public static void RegisterWithRoot<T>(this T actor) where T : Component, IRoomActor
        {
            var root = RoomRoot.Find(actor);
            //root.roomActors.AddUnique(actor);
        }
        public static void DeregisterFromRoot<T>(this T actor) where T : Component, IRoomActor
        {
            var root = RoomRoot.Find(actor);
            //root.roomActors.Remove(actor);
        }
        public static RoomRoot FindRoot<T>(this T actor) where T : Component, IRoomActor
        {
            if (actor == null || actor.gameObject.scene == null) return null;

            actor.gameObject.scene.GetRootGameObjects()[0].TryGetComponent(out RoomRoot res);
            return res;
        }
        public static bool FindRoot<T>(this T actor, out RoomRoot result) where T : Component, IRoomActor
        {
            result = null;
            return actor != null && actor.gameObject.scene != null && actor.gameObject.scene.GetRootGameObjects()[0].TryGetComponent(out result);
        }

    }
}