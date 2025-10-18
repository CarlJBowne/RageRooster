using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;
using System;





#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RageRooster.RoomSystem
{
    /// <summary>
    /// A Development-Time Asset defining a Room in the game world. <br/>
    /// </summary>
    [CreateAssetMenu(fileName = "Room", menuName = "ScriptableObjects/Room")]
    public class RoomAsset : ScriptableObject
    {
        #region Serialized Data

        /// <summary>
        /// The Display name for the room. Used in UI for denoting Save File location.
        /// </summary>
        [field: SerializeField] public string displayName { get; protected set; } = "INSERT_DISPLAY_NAME";
        /// <summary>
        /// The Area Asset this Room is a part of.
        /// </summary>
        [field: SerializeField] public AreaAsset area { get; protected set; }

        [field: SerializeField, Obsolete("Not Necessary.")] public Vector3 globalCenter { get; protected set; }
        /// <summary>
        /// The Scene Asset containing the contents of this Room.
        /// </summary>
        [field: SerializeField] public SceneReference scene { get; protected set; }
        [field: SerializeField] public RoomLOD lod { get; protected set; }

        /// <summary>
        /// The list of Entrance points into this room. The Player's position to these entrances is compared every few frames to determine when to load/unload the room.
        /// </summary>
        [field: SerializeField] public List<RoomEntrance.Data> entrances { get; protected set; } = new();

        #endregion

        #region Active Data
        /// <summary>
        /// The Active <see cref="RoomRoot" attached to the scene./>
        /// </summary>
        public RoomRoot root { get; protected set; }
        /// <summary>
        /// The possible states a room can be in.
        /// </summary>
        public enum RoomState
        {
            Null = -1,
            Lowest,
            LODS,
            Unloading,
            Loading,
            Present,
            Current
        }
        /// <summary>
        /// The current working state of this room.
        /// </summary>
        public RoomState state { get; protected set; } = RoomState.Null;

        /// <summary>
        /// The current ID of the LOD instance being shown. Currently only supports one LOD level.
        /// </summary>
        public int currentLOD { get; protected set; } = -1;
        #endregion

        /// <summary>
        /// Establishes a connection to the specified room root.
        /// </summary>
        /// <param name="root">The <see cref="RoomRoot"/> instance representing the room to connect to. Cannot be null.</param>
        public void Connect(RoomRoot root) => this.root = root;

        /// <summary>
        /// Enters the current room.
        /// </summary>
        public void Enter() => RoomManager.EnterRoom(this);
        internal void _Enter()
        {
            state = RoomState.Current;
        }
        internal void _Exit()
        {
            state = RoomState.Present;
        }

        /// <summary>
        /// Updates the state of the room based on the player's position and the current room state.
        /// </summary>
        public void Update()
        {
            if (state is RoomState.Current or RoomState.Unloading or RoomState.Loading) return;

            Vector3 player = PlayerMovementBody.PositionGet;
            if (state is RoomState.Present)
            {
                if (!WithinUnloadRange(player)) 
                    SceneUnload().Begin(area.root);
            }
            else
            {
                if (WithinLoadRange(player))
                {
                    SceneLoad().Begin(area.root);
                    return;
                }
                else if(state is RoomState.Lowest && WithinLodRange(player))
                {
                    state = RoomState.LODS;
                    lod.TurnOn();
                }
                else if (state is RoomState.LODS && !WithinLodRange(player))
                {
                    state = RoomState.Lowest;
                    lod.TurnOff();
                }
            }
        }

        #region Range Calculators
        private bool WithinLoadRange(Vector3 player)
        {
            // Early exit if there are no transitions
            if (entrances == null || entrances.Count == 0)
                return false;

            // Use foreach, but return immediately on first match
            foreach (RoomEntrance.Data item in entrances)
            {
                if (item.direction != Vector3.zero && Vector3.Dot(item.point - player, item.direction) < 0) continue;
                if (Vector3.SqrMagnitude(player - item.point) < item.loadRadius * item.loadRadius)
                    return true;
            }
            return false;
        }
        private bool WithinUnloadRange(Vector3 player)
        {
            // Early exit if there are no transitions
            if (entrances == null || entrances.Count == 0)
                return false;

            // Use foreach, but return immediately on first match
            foreach (RoomEntrance.Data item in entrances)
            {
                if (item.direction != Vector3.zero && Vector3.Dot(item.point - player, item.direction) < 0) continue;
                if (Vector3.SqrMagnitude(player - item.point) < item.unloadRadius * item.unloadRadius)
                    return true;
            }
            return false;
        }
        private bool WithinLodRange(Vector3 player)
        {
            // Early exit if there are no transitions
            if (entrances == null || entrances.Count == 0)
                return false;

            // Use foreach, but return immediately on first match
            foreach (RoomEntrance.Data item in entrances)
            {
                if (item.direction != Vector3.zero && Vector3.Dot(item.point - player, item.direction) < 0) continue;
                if (Vector3.SqrMagnitude(player - item.point) < item.lodRadius * item.lodRadius)
                    return true;
            }
            return false;
        }
        #endregion









        /// <summary>
        /// Prepares this specific room as the end-destination of the current transfer.
        /// </summary>
        public IEnumerator PrepEnter()
        {
            yield return SceneLoad();
            state = RoomState.Current;
        }
        /// <summary>
        /// Prepares this room as a room in the target area of the current transfer. <br/>
        /// If the player is within load range, fully loads the room.
        /// </summary>
        public IEnumerator PrepSurrounding()
        {
            if (this == RoomManager.currentRoom) yield break;
            Vector3 player = PlayerMovementBody.PositionGet;
            if (WithinLoadRange(player))
            {
                yield return SceneLoad();
                state = RoomState.Present;
            }
            else
            {
                if (WithinLodRange(player))
                {
                    state = RoomState.LODS;
                    yield return lod.Load();
                }
                else
                {
                    state = RoomState.Lowest;
                }
            }
        }



        /// <summary>
        /// Loads the full scene for this room. 
        /// </summary>
        public IEnumerator SceneLoad()
        {
            if (state >= RoomState.Loading) yield break;
            state = RoomState.Loading;

            yield return SceneOperationRoutine.Load(scene);
            if (root == null) yield return new WaitUntil(() => root != null);

            state = RoomState.Present;
        }
        /// <summary>
        /// Unloads the full scene for this room.
        /// </summary>
        public IEnumerator SceneUnload()
        {
            if (state <= RoomState.Unloading) yield break;
            state = RoomState.Unloading;

            yield return SceneOperationRoutine.Unload(scene);

            state = RoomState.LODS;
        }

        /// <summary>
        /// Completely unloads this room, includin both the scene and any LOD instances.
        /// </summary>
        /// <returns></returns>
        public IEnumerator CompleteUnload()
        {
            if (state > RoomState.Present)
            {
                yield return SceneUnload();
            }
            else if (state == RoomState.Unloading)
            {
                yield return new WaitUntil(() => state != RoomState.Unloading);
            }
            else if (state == RoomState.Loading)
            {
                yield return new WaitUntil(() => state != RoomState.Loading);
                yield return SceneUnload();
            }

            lod.CompleteUnload();
            state = RoomState.Null;
            currentLOD = -1;
        }

        private void OnDisable()
        {
            state = RoomState.Null;
        }

        /// <summary>
        /// A Level-of-Detail instance for a room, to be loaded when the player is within a certain range of any of the room's entrances. <br/>
        /// </summary>
        [System.Serializable]
        public class RoomLOD
        {
            public float range;
            public Prefab prefab;
            public GameObject instance;
            bool loaded = false;

            private AsyncInstantiateOperation currentOP;
            private CoroutinePlus currentCoroutine;


            public void TurnOn()
            {
                if (loaded)
                {
                    instance.SetActive(true);
                }
                else
                {
                    Load().Begin(RoomManager.currentArea.root);
                }
            }
            public void TurnOff()
            {
                if (loaded)
                {
                    instance.SetActive(false);
                }
                else
                {
                    CancelLoad();
                }
            }

            public IEnumerator Load()
            {
                if(prefab.readOnlyObject == null) yield break;

                if (loaded == true) yield break;
                currentOP = prefab.InstantiateAsync(RoomManager.currentArea.root.transform);

                if (currentOP == null) yield break;
                while (!currentOP.isDone) yield return null;

                instance = currentOP.Result[0] as GameObject;
                instance.SetActive(true);
                loaded = true;
            }
            public void CancelLoad()
            {
                if(currentOP == null || currentCoroutine == null) return;
                currentOP.Cancel();
                currentCoroutine.StopAuto();
            }
            public void Unload()
            {
                if (loaded == false) return;
                Destroy(instance);
                instance = null;
                loaded = false;
            }

            public void CompleteUnload()
            {
                instance = null;
                loaded = false;
            }
        }



#if UNITY_EDITOR

        [CustomEditor(typeof(RoomAsset))]
        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                RoomAsset roomAsset = (RoomAsset)target;

                // Draw Area link or orphan warning
                SerializedProperty areaProp = serializedObject.FindProperty(nameof(RoomAsset.area), backingField: true);
                AreaAsset areaAsset = areaProp.objectReferenceValue as AreaAsset;

                GUILayout.Space(8);

                if (areaAsset != null)
                {
                    GUIStyle linkStyle = new GUIStyle(EditorStyles.label);
                    linkStyle.normal.textColor = new Color(0.2f, 0.5f, 1f);
                    linkStyle.fontStyle = FontStyle.Bold;

                    if (GUILayout.Button($"Area: {areaAsset.displayName}", linkStyle))
                    {
                        Selection.activeObject = areaAsset;
                        EditorGUIUtility.PingObject(areaAsset);
                    }
                }
                else
                {
                    GUIStyle redStyle = new GUIStyle(EditorStyles.label);
                    redStyle.normal.textColor = Color.red;
                    redStyle.fontStyle = FontStyle.Bold;
                    GUILayout.Label("ORPHAN ROOM, PLEASE ADD TO AREA", redStyle);
                }

                GUILayout.Space(8);

                serializedObject.Update();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.displayName), backingField: true));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.scene), backingField: true));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.lod), backingField: true));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(roomAsset, "Modified Room Asset");
                    EditorUtility.SetDirty(roomAsset);
                }

                // Display foldable, uneditable list of transitions
                SerializedProperty transitionsProp = serializedObject.FindProperty(nameof(RoomAsset.entrances), backingField: true);
                bool transitionsFoldout = EditorPrefs.GetBool("RoomAsset_EntrancesFoldout", true);
                transitionsFoldout = EditorGUILayout.Foldout(transitionsFoldout, "Entrances", true);
                EditorPrefs.SetBool("RoomAsset_EntrancesFoldout", transitionsFoldout);

                if (transitionsFoldout)
                {
                    if (transitionsProp != null && transitionsProp.isArray)
                    {
                        int count = transitionsProp.arraySize;
                        if (count == 0)
                        {
                            EditorGUILayout.LabelField("No Entrances attached.");
                        }
                        else
                        {
                            EditorGUI.indentLevel++;
                            for (int i = 0; i < count; i++)
                            {
                                SerializedProperty itemProp = transitionsProp.GetArrayElementAtIndex(i);
                                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.PropertyField(itemProp, new($"Entrance {i + 1}"), true);
                                EditorGUI.EndDisabledGroup();
                                EditorGUILayout.EndVertical();
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Entrances property not found.");
                    }
                }
            }
            protected void OnDisable()
            {
                AssetDatabase.SaveAssetIfDirty(target);
            }

            [MenuItem("File/Create Room", priority = 0)]
            public static void CREATE_BEGIN() => CreateRoomPopupWindow.Show(CREATE);
            static void CREATE(AreaAsset area, string name)
            {
                string roomPath = $"Assets/World/Rooms/{area.name}/{name}.asset";
                string scenePath = $"Assets/World/Rooms/{area.name}/{name}_Scene.unity";

                //Create directory if it doesn't exist

                if (!Directory.Exists(Path.GetDirectoryName(roomPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(roomPath));
                    AssetDatabase.Refresh();
                }

                RoomAsset room = ScriptableObject.CreateInstance<RoomAsset>();
                AssetDatabase.CreateAsset(room, roomPath);

                if (!AssetDatabase.CopyAsset("Assets/Editor/RoomTemplate.unity", scenePath)) return;

                room.displayName = name;
                room.area = area;
                room.scene = new(AssetDatabase.LoadAssetAtPath<Object>(scenePath));
                EditorUtility.SetDirty(room);
                area.rooms.Add(room);
                EditorUtility.SetDirty(area);

                AssetDatabase.SaveAssets();

                // Open, attach, save, and close scene
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                RoomRoot.Editor.AttachAsset(scene.GetRootGameObjects()[0].GetComponent<RoomRoot>(), room);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                EditorSceneManager.CloseScene(scene, true);

                Debug.Log($"Successfully created new Room: {name} under Area: {area.name}. Note that its Scene cannot be automatically registered in the build settings, YOU have to do that.");
            }

            private class CreateRoomPopupWindow : EditorWindow
            {
                private AreaAsset areaAsset;
                private string roomName = "";
                private System.Action<AreaAsset, string> onCreate;

                public static void Show(System.Action<AreaAsset, string> onCreate)
                {
                    var window = ScriptableObject.CreateInstance<CreateRoomPopupWindow>();
                    window.titleContent = new GUIContent("Create Room");
                    window.position = new Rect(Screen.width / 2, Screen.height / 2, 350, 100);
                    window.onCreate = onCreate;
                    window.ShowUtility();
                }

                private void OnGUI()
                {
                    GUILayout.Label("Create New Room", EditorStyles.boldLabel);
                    areaAsset = (AreaAsset)EditorGUILayout.ObjectField("Area Asset", areaAsset, typeof(AreaAsset), false);
                    roomName = EditorGUILayout.TextField("Room Name", roomName);

                    EditorGUI.BeginDisabledGroup(areaAsset == null || string.IsNullOrWhiteSpace(roomName));
                    if (GUILayout.Button("Create"))
                    {
                        Close();
                        onCreate?.Invoke(areaAsset, roomName);
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
        }

        

#endif


    }
}