using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Linq;
using System.Reflection;
using SLS.ISingleton;

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

                var types = typeof(IRoomObject).GetAllChildTypes(true);

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

#endif
    }
    /// <summary>
    /// An interface representing objects with an important connection to the <see cref="RoomRoot"/>/<see cref="RoomAsset"/> they belong to.
    /// <br/> Add the OnSaveSceneSet method to override what happens when the Room's scene is saved in editor.
    /// <br/> (See script for example.)
    /// </summary>
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

        //private void OnSaveScene(RoomRoot root)
        //private static void OnSaveSceneSet(RoomRoot root, List<CLASSTYPE> list)
    }
}