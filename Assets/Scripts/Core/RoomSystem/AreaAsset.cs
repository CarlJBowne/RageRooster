using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using RageRooster.Systems.SaveSystem;
using RageRooster.Systems.SaveSystem.Flags;
using FMODUnity;
using Utilities.Xtensions;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
#endif

namespace RageRooster.RoomSystem
{
    /// <summary>
    /// A Development-Time Asset defining an Area in the game world. <br/>
    /// </summary>
    [CreateAssetMenu(fileName = "Area", menuName = "ScriptableObjects/Area")]
    public class AreaAsset : ScriptableObject
    {
        #region Config Fields
        /// <summary>
        /// The Display Name of this Area, Used in UI for denoting Save File location.
        /// </summary>
        [field: SerializeField] public string displayName { get; protected set; } = "INSERT_DISPLAY_NAME";
        /// <summary>
        /// The Scene containing a basic shell of the area, functioning as a 0th level-of-detail for every room in the area.
        /// </summary>
        [field: SerializeField] public SceneReference shellScene { get; protected set; }
        /// <summary>
        /// The <see cref="RoomAsset"/>s that make up this Area.
        /// </summary>
        [field: SerializeField] public List<RoomAsset> rooms { get; protected set; } = new();

        /// <summary>
        /// The default music to play while in this area.
        /// </summary>
        [field: SerializeField] public EventReference music { get; protected set; }

        /// <summary>
        /// The Dev-Defined default flags for this area. These are cloned into the active <see cref="SaveData"/> when a new game is started.
        /// </summary>
        [field: SerializeField] public SavedFlagSet flagDefaults { get; protected set; }
        #endregion

        #region Active Data
        /// <summary>
        /// The currently active <see cref="AreaRoot"/> instance for this area found in the <see cref="shellScene"/>.
        /// </summary>
        public AreaRoot root { get; protected set; }

        /// <summary>
        /// Gets the current state of this Area's <see cref="shellScene"/>
        /// </summary>
        public SceneState state { get; protected set; } = SceneState.Valid;

        /// <summary>
        /// Is this the currently loaded area?
        /// </summary>
        public bool isCurrent { get; protected set; }
        #endregion


        /// <summary>
        /// Loads this area's <see cref="shellScene"/> and prepares all rooms for use.
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadArea()
        {
            state = SceneState.Loading;

            yield return SceneOperationRoutine.Load(shellScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            if (root == null) yield return new WaitUntil(() => root != null);

            state = SceneState.Loaded;
            for (int i = 0; i < rooms.Count; i++)
                PlayerMovementBody.MovingUpdateAction += rooms[i].Update;
        }

        /// <summary>
        /// Establishes a connection to the specified <see cref="AreaRoot"/>.
        /// </summary>
        public void Connect(AreaRoot root)
        {
            this.root = root;
            if (root.roomLowestLods == null) return;
            for (int i = 0; i < root.roomLowestLods.Length; i++)
            {
                if (root.roomLowestLods[i] == null) continue;
                rooms[i].shellLodPiece = root.roomLowestLods[i];
            }
        }

        /// <summary>
        /// Unloads this area's <see cref="shellScene"/> and all rooms within it.
        /// </summary>
        /// <returns></returns>
        public IEnumerator UnloadArea()
        {
            state = SceneState.Unloading;
            foreach (RoomAsset room in rooms)
            {
                PlayerMovementBody.MovingUpdateAction -= room.Update;
                yield return room.CompleteUnload();
            }

            yield return SceneOperationRoutine.Unload(shellScene);

            state = SceneState.Valid;
        }



#if UNITY_EDITOR

        [CustomEditor(typeof(AreaAsset))]
        public class Editor : UnityEditor.Editor
        {
            public override VisualElement CreateInspectorGUI()
            {
                roomsList = CreateRoomsList();





                return base.CreateInspectorGUI();
            }

            private ReorderableList roomsList;

            public override void OnInspectorGUI()
            {
                AreaAsset areaAsset = (AreaAsset)target;

                serializedObject.Update();
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AreaAsset.displayName).BackingField()));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AreaAsset.shellScene).BackingField()), true);

                SerializedProperty roomsProperty = serializedObject.FindProperty("Rooms".BackingField());
                roomsList.DoLayoutList();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AreaAsset.music).BackingField()));
                var flagSetProp = serializedObject.FindProperty(nameof(AreaAsset.flagDefaults).BackingField());
                EditorGUILayout.PropertyField(flagSetProp);
                if (flagSetProp.objectReferenceValue == null && GUILayout.Button("Create and Attach FlagSet")) CreateFlagSet(areaAsset);

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(areaAsset, "Modified Area Asset");
                    EditorUtility.SetDirty(areaAsset);
                }
            }

            protected void OnDisable()
            {
                AssetDatabase.SaveAssetIfDirty(target);
            }

            private ReorderableList CreateRoomsList()
            {
                SerializedProperty roomsProperty = serializedObject.FindProperty(nameof(AreaAsset.rooms).BackingField());
                ReorderableList list = new ReorderableList(serializedObject, roomsProperty, true, true, true, true);
                list.draggable = true;
                list.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Rooms"); };

                list.drawElementCallback = DrawElementCallback;

                list.elementHeightCallback = (int index) =>
                {
                    return EditorGUIUtility.singleLineHeight; // Adjust height as needed
                };

                list.onAddCallback = (ReorderableList l) =>
                {
                    // Inside the onAddCallback for the ReorderableList in CreateRoomsList()
                    list.onAddCallback = (ReorderableList l) =>
                    {
                        // Detect if Shift is held
                        bool shiftHeld = (Event.current != null) && (Event.current.shift);

                        if (shiftHeld)
                        {
                            // Add a null slot to the rooms list
                            roomsProperty.arraySize++;
                            roomsProperty.GetArrayElementAtIndex(roomsProperty.arraySize - 1).objectReferenceValue = null;
                            serializedObject.ApplyModifiedProperties();
                            return;
                        }

                        // Generate a unique name for the new RoomAsset
                        string baseName = "Room";
                        int suffix = 1;
                        string assetName;
                        string areaAssetDirectory = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(target));
                        string areaFolderName = ((AreaAsset)target).name;
                        string areaFolderPath = System.IO.Path.Combine(areaAssetDirectory, areaFolderName);

                        // Ensure the folder exists
                        if (!AssetDatabase.IsValidFolder(areaFolderPath)) AssetDatabase.CreateFolder(areaAssetDirectory, areaFolderName);

                        do
                        {
                            assetName = $"{baseName}{suffix}";
                            suffix++;
                        }
                        while (AssetDatabase.FindAssets(assetName, new[] { areaFolderPath }).Length > 0);

                        RoomAsset newRoom = ScriptableObject.CreateInstance<RoomAsset>();
                        newRoom.name = assetName; // Set the name of the asset

                        string roomAssetPath = System.IO.Path.Combine(areaFolderPath, $"{assetName}.asset");
                        AssetDatabase.CreateAsset(newRoom, roomAssetPath);
                        roomsProperty.arraySize++;
                        RegisterRoom(new(newRoom), serializedObject, roomsProperty.GetArrayElementAtIndex(roomsProperty.arraySize - 1));
                        Undo.RegisterCreatedObjectUndo(newRoom, "Added New Object");
                        AssetDatabase.SaveAssets();
                        serializedObject.ApplyModifiedProperties();
                    };
                };

                list.onRemoveCallback = (ReorderableList l) =>
                {
                    if (roomsProperty.arraySize == 0) return;
                    int index = l.index;
                    if (index < 0 || index >= roomsProperty.arraySize) return;

                    SerializedProperty element = roomsProperty.GetArrayElementAtIndex(index);
                    RoomAsset roomObj = element.objectReferenceValue as RoomAsset;
                    if (element.objectReferenceValue != null)
                    {
                        bool shouldDelete = EditorUtility.DisplayDialog(
                            "Remove Room",
                            $"Do you want to delete the RoomAsset '{roomObj.name}' from the project?\n\n" +
                            "Click 'Delete' to remove the asset file, or 'Keep' to just remove the reference.",
                            "Delete",
                            "Keep"
                        );
                        UnregisterRoom(new(element.objectReferenceValue), l.serializedProperty, index, true);
                        if (shouldDelete)
                        {
                            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(roomObj));
                            AssetDatabase.SaveAssets();
                        }
                    }
                    else
                    {
                        roomsProperty.DeleteArrayElementAtIndex(index);
                    }

                    serializedObject.ApplyModifiedProperties();
                };

                list.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Rooms");

                    if (!rect.Contains(Event.current.mousePosition)) return;

                    if (Event.current.type != EventType.DragUpdated && Event.current.type != EventType.DragPerform) return;
                    if (DragAndDrop.objectReferences.Length == 0 || !(DragAndDrop.objectReferences[0] is RoomAsset)) return;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (Event.current.type != EventType.DragPerform) return;
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is RoomAsset roomAsset)
                        {

                            if (roomAsset.area != null && roomAsset.area != (AreaAsset)target) //Target Room already registered to another Area
                            {
                                bool res = EditorUtility.DisplayDialog(
                                    "Move Room?",
                                    $"RoomAsset '{roomAsset.name}' is already registered to AreaAsset '{roomAsset.area.name}'.\n" +
                                    "Rooms should not be registered under more than one area.\n" +
                                    "Would you like to move this Room to the new Area?",
                                    "Move", "Cancel"
                                );
                                if (!res) return;
                                else UnregisterRoom(new(roomAsset));
                            }

                            // Add the dragged RoomAsset to the list
                            roomsProperty.arraySize++;
                            RegisterRoom(new SerializedObject(roomAsset), serializedObject, roomsProperty.GetArrayElementAtIndex(roomsProperty.arraySize - 1));
                        }
                    }
                    serializedObject.ApplyModifiedProperties();
                };



                return list;
            }


            void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                SerializedProperty listProperty = roomsList.serializedProperty;
                SerializedProperty element = listProperty.GetArrayElementAtIndex(index);

                RoomAsset oldRoom = element.objectReferenceValue as RoomAsset;
                AreaAsset This = (AreaAsset)target;

                EditorGUI.BeginChangeCheck();
                RoomAsset newRoom = (RoomAsset)EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    oldRoom,
                    typeof(RoomAsset),
                    false
                );
                if (EditorGUI.EndChangeCheck()) ListOperation();

                void ListOperation()
                {
                    var oldRoomS = oldRoom != null ? new SerializedObject(oldRoom) : null;
                    var newRoomS = newRoom != null ? new SerializedObject(newRoom) : null;

                    // Gather all possible scenarios and decisions
                    bool addAdditional = false;
                    bool deleteOld = false;

                    if (oldRoom == newRoom) return; //No Chance, nothing to do.
                    if (newRoom != null) //NewRoom isnt Null
                    {
                        if (newRoom != null && This.rooms.Contains(newRoom) && newRoom != oldRoom) //Room Already Exists in same list
                        {
                            EditorUtility.DisplayDialog(
                                "Room Already Exists",
                                $"RoomAsset '{newRoom.name}' is already in this Area's room list.",
                                "OK"
                            );
                            return;
                        }
                        if (newRoom.area != null && newRoom.area != This) //Target Room already registered to another Area
                        {
                            bool res = EditorUtility.DisplayDialog(
                                "Move Room?",
                                $"RoomAsset '{newRoom.name}' is already registered to AreaAsset '{newRoom.area.name}'.\n" +
                                "Rooms should not be registered under more than one area.\n" +
                                "Would you like to move this Room to the new Area?",
                                "Move", "Cancel"
                            );
                            if (!res) return;
                        }
                        if (oldRoom != null) //Slot had another room in it.
                        {
                            int res = EditorUtility.DisplayDialogComplex(
                                "Replace Room?",
                                $"Do you want to replace '{oldRoom.name}' with '{newRoom.name}'?",
                                "Replace", "Cancel", "Add in new slot instead"
                            );
                            if (res == 0)
                            {
                                addAdditional = false;
                            }
                            else if (res == 2)
                            {
                                addAdditional = true;
                            }
                            else return;
                        }
                    }
                    else //Emptying Slot
                    {
                        int emptyingChoice = EditorUtility.DisplayDialogComplex(
                            "Remove Room?",
                            $"Do you want to remove '{oldRoom.name}' from this area or delete it from the project?",
                            "Remove From Area", "Cancel", "Delete from Project"
                        );
                        if (emptyingChoice == 0)
                        {

                        }
                        else if (emptyingChoice == 2)
                        {
                            deleteOld = true;
                        }
                        else return;
                    }




                    if (!addAdditional)
                    {
                        if (oldRoom != null)
                        {
                            UnregisterRoom(oldRoomS, listProperty, index);
                            if (deleteOld)
                            {
                                DestroyImmediate(oldRoom, true);
                                AssetDatabase.SaveAssets();
                            }
                        }
                        if (newRoom != null)
                        {
                            if (newRoom.area != null && newRoom.area != This) UnregisterRoom(newRoomS);
                            RegisterRoom(newRoomS, serializedObject, listProperty.GetArrayElementAtIndex(index));
                        }
                    }
                    else
                    {
                        listProperty.arraySize++;
                        RegisterRoom(newRoomS, serializedObject, listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1));
                    }

                    listProperty.serializedObject.ApplyModifiedProperties();
                }
            }



            protected void RegisterRoom(SerializedObject room, SerializedObject area, SerializedProperty listSlot)
            {
                room.FindProperty(nameof(RoomAsset.area).BackingField()).objectReferenceValue = area.targetObject;
                room.ApplyModifiedProperties(); // Ensure changes are applied to the SerializedObject  
                listSlot.objectReferenceValue = room.targetObject;
            }
            protected void UnregisterRoom(SerializedObject room, SerializedProperty listProperty, int index, bool deleteSlot = false, bool deleteFile = false)
            {
                listProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
                room.FindProperty(nameof(RoomAsset.area).BackingField()).objectReferenceValue = null;
                room.ApplyModifiedProperties();
                if (deleteSlot) listProperty.DeleteArrayElementAtIndex(index);
            }
            protected void UnregisterRoom(SerializedObject room)
            {
                SerializedProperty areaProp = room.FindProperty(nameof(RoomAsset.area).BackingField());
                var area = areaProp.objectReferenceValue as AreaAsset;
                int ID = area.rooms.IndexOf(room.targetObject as RoomAsset);

                area.rooms.Remove(room.targetObject as RoomAsset);
                areaProp.objectReferenceValue = null;
                room.ApplyModifiedProperties();
            }

            [MenuItem("File/Create Area", priority = 0)]
            public static void CREATE_BEGIN() => CreateAreaPopupWindow.Show(CREATE);
            public static void CREATE(string name)
            {

                string assetPath = $"Assets/World/Areas/{name}.asset";
                string scenePath = $"Assets/World/Areas/{name}_Scene.unity";


                // Create AreaAsset
                var area = ScriptableObject.CreateInstance<AreaAsset>();
                AssetDatabase.CreateAsset(area, assetPath);
                AreaRegistry.Editor_AddArea(area);

                // Create scene from template
                if (!AssetDatabase.CopyAsset("Assets/Editor/AreaTemplate.unity", scenePath)) return;

                // Set up AreaAsset properties
                area.displayName = name;
                area.shellScene = new SceneReference(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scenePath));
                CreateFlagSet(area);
                EditorUtility.SetDirty(area);

                // Open, attach, save, and close scene
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                AreaRoot.Editor.AttachAsset(scene.GetRootGameObjects()[0].GetComponent<AreaRoot>(), area);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                EditorSceneManager.CloseScene(scene, true);

                Directory.CreateDirectory($"Assets/World/Rooms/{name}");
                AssetDatabase.Refresh();

                Debug.Log($"Successfully created new Area: {name}. Note that its Scene cannot be automatically registered in the build settings, YOU have to do that.");
            }

            public static void CreateFlagSet(AreaAsset This)
            {
                // Create new SavedFlagSet asset in the same folder as AreaAsset
                string flagSetPath = System.IO.Path.Combine("Assets/World/Areas/AreaFlags", $"{This.name}_FlagDefaults.asset");

                var flagSet = ScriptableObject.CreateInstance<SavedFlagSet>();
                AssetDatabase.CreateAsset(flagSet, flagSetPath);
                AssetDatabase.SaveAssets();

                This.flagDefaults = flagSet;
                Undo.RegisterCreatedObjectUndo(flagSet, "Create FlagSet");
                EditorUtility.SetDirty(This);
            }

            private class CreateAreaPopupWindow : EditorWindow
            {
                private string roomName = "";
                private System.Action<string> onCreate;

                public static void Show(System.Action<string> onCreate)
                {
                    var window = ScriptableObject.CreateInstance<CreateAreaPopupWindow>();
                    window.titleContent = new GUIContent("Create Area");
                    window.position = new Rect(Screen.width / 2, Screen.height / 2, 350, EditorGUIUtility.singleLineHeight * 3);
                    window.onCreate = onCreate;
                    window.ShowUtility();
                }

                private void OnGUI()
                {
                    GUILayout.Label("Create New Area", EditorStyles.boldLabel);
                    roomName = EditorGUILayout.TextField("Area Name", roomName);

                    EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(roomName));
                    if (GUILayout.Button("Create"))
                    {
                        Close();
                        onCreate?.Invoke(roomName);
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
        }

#endif
    }
}