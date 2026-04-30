using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using System.Linq;
using UnityEngine.UIElements;
using Utilities.Xtensions.VisualElements;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
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

        /// <summary>
        /// The Scene Asset containing the contents of this Room.
        /// </summary>
        [field: SerializeField] public SceneReference scene { get; protected set; }
        [field: SerializeField] public RoomLOD lod { get; protected set; }

        /// <summary>
        /// The list of Entrance points into this room. The Player's position to these entrances is compared every few frames to determine when to load/unload the room.
        /// </summary>
        [field: SerializeField] public List<RoomEntrance.Data> entrances { get; protected set; } = new();

#if UNITY_EDITOR
        [field: SerializeField] public List<string> spawnPointNames { get; protected set; } = new();
#endif

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


        public GameObject shellLodPiece { get; internal set; }
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

            if (entrances == null || entrances.Count == 0) return;


            UpdateDistances(out int entranceID, out int stripScore);

            if (state is RoomState.Present)
            {
                if (stripScore < 3) SceneUnload().Begin(area.root);
            }
            else
            {
                if (stripScore == 3) SceneLoad().Begin(area.root);
                else if (state is RoomState.Lowest && stripScore > 0)
                {
                    state = RoomState.LODS;
                    lod.TurnOn();
                    if (shellLodPiece) shellLodPiece.SetActive(false);
                }
                else if (state is RoomState.LODS && stripScore == 0)
                {
                    state = RoomState.Lowest;
                    lod.TurnOff();
                    if (shellLodPiece) shellLodPiece.SetActive(true);
                }
            }
        }

        void UpdateDistances(out int bestEntranceID, out int topStripScore)
        {
            bestEntranceID = 0;
            topStripScore = -5;

            for (int i = 0; i < entrances.Count; i++)
            {
                entrances[i].UpdateDistance();
                int iStrip = entrances[i].strip;

                if (iStrip == -2) continue;
                if (iStrip == -1 && state is RoomState.Present or RoomState.Loading) iStrip = 3;

                if (iStrip > topStripScore)
                {
                    topStripScore = iStrip;
                    bestEntranceID = i;
                }
                else if (iStrip == topStripScore && entrances[i] < entrances[bestEntranceID])
                    bestEntranceID = i;
            }
        }
        // 3 = Within Load Radius
        // 2 = Within Unload Radius
        // 1 = Within LOD Radius
        // 0 = Outside




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

            UpdateDistances(out int entranceID, out int stripScore);

            if (stripScore > 2)
            {
                yield return SceneLoad();
                state = RoomState.Present;
            }
            else
            {
                if (stripScore > 0)
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

            if (shellLodPiece && shellLodPiece.activeSelf) shellLodPiece.SetActive(false);
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
            private Coroutine currentCoroutine;


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
                if (prefab.readOnlyObject == null) yield break;

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
                if (currentOP == null || currentCoroutine == null) return;
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
            public override VisualElement CreateInspectorGUI()
            {
                serializedObject.Update();

                var root = new VisualElement();

                // Area link or orphan warning
                SerializedProperty areaProp = serializedObject.FindProperty(nameof(RoomAsset.area), backingField: true);
                AreaAsset areaAsset = areaProp != null ? areaProp.objectReferenceValue as AreaAsset : null;

                if (areaAsset != null)
                {
                    var areaButton = new Label($"Area: {areaAsset.displayName}")
                    {
                        style =
                        {
                            color = new StyleColor(new Color(0.2f, 0.5f, 1f)),
                            unityFontStyleAndWeight = FontStyle.Bold,
                            alignSelf = Align.FlexStart,
                        }
                    };
                    areaButton.Highlighter(.3f);
                    areaButton.RegisterCallback<ClickEvent>(PING);
                    root.Add(areaButton);


                    void PING(ClickEvent _)
                    {
                        Selection.activeObject = areaAsset;
                        EditorGUIUtility.PingObject(areaAsset);

                    }
                }
                else
                {
                    var orphanLabel = new Label("ORPHAN ROOM, PLEASE ADD TO AREA");
                    orphanLabel.style.color = new StyleColor(Color.red);
                    orphanLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    orphanLabel.style.alignSelf = Align.FlexStart;
                    root.Add(orphanLabel);
                }

                // Spacer
                var spacer = new VisualElement();
                spacer.style.height = 8;
                root.Add(spacer);

                // Editable properties
                var displayNameProp = serializedObject.FindProperty(nameof(RoomAsset.displayName), backingField: true);
                var sceneProp = serializedObject.FindProperty(nameof(RoomAsset.scene), backingField: true);
                var lodProp = serializedObject.FindProperty(nameof(RoomAsset.lod), backingField: true);

                if (displayNameProp != null)
                    root.Add(new PropertyField(displayNameProp, "Display Name"));
                if (sceneProp != null)
                    root.Add(new PropertyField(sceneProp, "Scene"));
                if (lodProp != null)
                    root.Add(new PropertyField(lodProp, "LOD"));

                // Entrances foldout (read-only)
                SerializedProperty transitionsProp = serializedObject.FindProperty(nameof(RoomAsset.entrances), backingField: true);
                bool transitionsFoldoutState = EditorPrefs.GetBool("RoomAsset_EntrancesFoldout", true);
                var entrancesFoldout = new Foldout
                {
                    text = "Entrances",
                    value = transitionsFoldoutState
                };
                entrancesFoldout.RegisterValueChangedCallback(evt => EditorPrefs.SetBool("RoomAsset_EntrancesFoldout", evt.newValue));

                if (transitionsProp != null && transitionsProp.isArray)
                {
                    int count = transitionsProp.arraySize;
                    if (count == 0)
                    {
                        entrancesFoldout.Add(new Label("No Entrances attached."));
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                        {
                            SerializedProperty itemProp = transitionsProp.GetArrayElementAtIndex(i);
                            var container = new VisualElement();
                            container.style.marginTop = 4;
                            container.style.marginBottom = 4;
                            container.style.paddingLeft = 4;
                            container.style.paddingRight = 4;
                            container.style.unityBackgroundImageTintColor = Color.clear;
                            container.style.borderTopWidth = 1;
                            container.style.borderBottomWidth = 1;
                            container.style.borderLeftWidth = 1;
                            container.style.borderRightWidth = 1;
                            container.style.borderTopColor = new StyleColor(new Color(0.0f, 0.0f, 0.0f, 0.0f));
                            var pf = new PropertyField(itemProp, $"Entrance {i + 1}");
                            // Make read-only
                            pf.SetEnabled(false);
                            container.Add(pf);
                            entrancesFoldout.Add(container);
                        }
                    }
                }
                else
                {
                    entrancesFoldout.Add(new Label("Entrances property not found."));
                }

                root.Add(entrancesFoldout);


                Foldout spawnFoldout = new()
                {
                    text = "SpawnPoint Names",
                    value = true
                };
                root.Add(spawnFoldout);

                // Spawn point names display (editable)
                SerializedProperty spawnNamesProp = serializedObject.FindProperty(nameof(RoomAsset.spawnPointNames), backingField: true);
                for (int i = 0; i < spawnNamesProp.arraySize; i++)
                    spawnFoldout.Add(new Label($"{i} : {spawnNamesProp.GetArrayElementAtIndex(i).stringValue}"));
                

                // Bind fields to serializedObject - PropertyField does this automatically.
                // Ensure changes are applied when serialization changes occur.
                // Subscribe to focus out on root to apply modified properties (defensive).
                root.RegisterCallback<FocusOutEvent>(evt =>
                {
                    if (serializedObject != null)
                    {
                        serializedObject.ApplyModifiedProperties();
                        Undo.RecordObject(target, "Modified Room Asset");
                        EditorUtility.SetDirty(target);
                    }
                });

                // Also ensure ApplyModifiedProperties when inspector is rebuilt / returned
                root.style.flexDirection = FlexDirection.Column;

                return root;
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
                room.scene = new(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scenePath));
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
                    CreateRoomPopupWindow window = ScriptableObject.CreateInstance<CreateRoomPopupWindow>();
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

            private class OpenRoomPopupWindow : EditorWindow
            {
                [MenuItem("File/Open Rooms", priority = -1)]
                public static new void Show()
                {
                    OpenRoomPopupWindow w = ScriptableObject.CreateInstance<OpenRoomPopupWindow>();
                    w.titleContent = new("Choose Room to Open");
                    w.ShowModalUtility();
                }

                private void OnEnable()
                {
                    foreach (AreaAsset area in AreaRegistry.GetAll().ToList())
                    {
                        Label areaLabel = new(area.displayName);
                        rootVisualElement.Add(areaLabel);
                        areaLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                        foreach (var room in area.rooms)
                        {
                            Button roomLabel = new(OpenRoom)
                            {
                                text = room.displayName,
                                style =
                                {
                                    backgroundColor = Color.clear,
                                    borderBottomWidth = 0,
                                    borderRightWidth = 0,
                                    borderLeftWidth = 0,
                                    borderTopWidth = 0,
                                    color = Color.cornflowerBlue,
                                    unityTextAlign = TextAnchor.MiddleLeft
                                }
                            };
                            roomLabel.Highlighter(.1f);
                            rootVisualElement.Add(roomLabel);
                            void OpenRoom()
                            {
                                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(room.scene.asset));
                                Close();
                            }
                        }
                    }
                }

            }


        }



#endif


    }
}