using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using RageRooster.Core;
using RageRooster.Core.Save;
using FMODUnity;
using System;
using SLS.SaveData;
using SLS.ListUtilities;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using SLS.ListUtilities.Editor;
#endif

namespace RageRooster.World
{
    /// <summary>
    /// A Development-Time Asset defining an Area in the game world. <br/>
    /// </summary>
    [CreateAssetMenu(fileName = "Area", menuName = "ScriptableObjects/Area")]
    public class AreaAsset : SceneSO//, IAreaAsset
    {
        #region Config Fields
        /// <summary>
        /// The Display Name of this Area, Used in UI for denoting Save File location.
        /// </summary>
        [field: SerializeField] public string displayName { get; protected set; } = "INSERT_DISPLAY_NAME";
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
        [field: SerializeField] public Flag.Collection flagDefaults { get; protected set; } = new();
        #endregion

        #region Active Data
        /// <summary>
        /// The currently active <see cref="AreaRoot"/> instance for this area found in the <see cref="shellScene"/>.
        /// </summary>
        public AreaRoot root { get; protected set; }

        /// <summary>
        /// Is this the currently loaded area?
        /// </summary>
        public bool isCurrent { get; protected set; }
        #endregion

        protected override void OnFinishLoad()
        {
            for (int i = 0; i < rooms.Count; i++)
                if (IPlayer.Present) IPlayer.Self.OnMovingUpdate += rooms[i].Update;

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
        public override IEnumerator UnloadRoutine()
        {
            // Unsubscribe and fully unload contained rooms, then unload the shell scene using SceneSO.
            foreach (RoomAsset room in rooms)
            {
                if (IPlayer.Present) IPlayer.Self.OnMovingUpdate -= room.Update;
                yield return room.CompleteUnload();
            }

            yield return base.UnloadRoutine();
        }


        public RoomAsset GetRoom(string name)
        {
            for (int i = 0; i < rooms.Count; i++)
                if (rooms[i].name == name)
                    return rooms[i];
            return null;
        }
        public RoomAsset GetRoom(int i) => rooms[i];


#if UNITY_EDITOR

        [CustomEditor(typeof(AreaAsset))]
        public class Editor : UnityEditor.Editor
        {
            private SuperRoomsList roomsList;

            public override VisualElement CreateInspectorGUI()
            {
                // Root element
                var root = new VisualElement();
                root.style.paddingLeft = 4;
                root.style.paddingRight = 4;

                serializedObject.Update();

                // Display simple fields using property fields so bindings/undo work automatically
                var displayNameProp = serializedObject.FindBackingField(nameof(AreaAsset.displayName));
                var sceneProp = serializedObject.FindBackingField(nameof(AreaAsset.Scene));
                var musicProp = serializedObject.FindProperty(nameof(AreaAsset.music).BackingField());
                var flagsProp = serializedObject.FindBackingField(nameof(AreaAsset.flagDefaults));
                var roomsProp = serializedObject.FindBackingField(nameof(AreaAsset.rooms));

                if (displayNameProp != null)
                {
                    var pf = new PropertyField(displayNameProp);
                    pf.Bind(serializedObject);
                    root.Add(pf);
                }

                if (sceneProp != null)
                {
                    var sceneField = new PropertyField(sceneProp);
                    sceneField.Bind(serializedObject);
                    root.Add(sceneField);
                }

                // Rooms super list (uses SLS SuperList system)
                roomsList = new SuperRoomsList(roomsProp, this);
                roomsList.style.marginTop = 6;
                root.Add(roomsList);

                if (musicProp != null)
                {
                    var musicField = new PropertyField(musicProp);
                    musicField.Bind(serializedObject);
                    root.Add(musicField);
                }

                if (flagsProp != null)
                {
                    var flagsField = new PropertyField(flagsProp);
                    flagsField.Bind(serializedObject);
                    root.Add(flagsField);
                }

                serializedObject.ApplyModifiedProperties();

                return root;
            }

            protected void OnDisable()
            {
                AssetDatabase.SaveAssetIfDirty(target);
            }

            // Keep original helper methods; Rooms list will call into these to keep behavior identical
            protected void RegisterRoom(SerializedObject room, SerializedObject area, SerializedProperty listSlot)
            {
                room.FindBackingField(nameof(RoomAsset.area)).objectReferenceValue = area.targetObject;
                room.ApplyModifiedProperties(); // Ensure changes are applied to the SerializedObject  
                listSlot.objectReferenceValue = room.targetObject;
            }
            protected void UnregisterRoom(SerializedObject room, SerializedProperty listProperty, int index, bool deleteSlot = false, bool deleteFile = false)
            {
                listProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
                room.FindBackingField(nameof(RoomAsset.area)).objectReferenceValue = null;
                room.ApplyModifiedProperties();
                if (deleteSlot) listProperty.DeleteArrayElementAtIndex(index);
            }
            protected void UnregisterRoom(SerializedObject room)
            {
                SerializedProperty areaProp = room.FindBackingField(nameof(RoomAsset.area));
                var area = areaProp.objectReferenceValue as AreaAsset;
                int ID = area.rooms.IndexOf(room.targetObject as RoomAsset);

                area.rooms.Remove(room.targetObject as RoomAsset);
                areaProp.objectReferenceValue = null;
                room.ApplyModifiedProperties();
            }

            // The UIElements-based SuperList implementation for Rooms.
            private class SuperRoomsList : SuperList<SuperRoomsList, SuperRoomsListItem, RoomAsset>
            {
                private Editor ownerEditor;
                private SerializedObject areaSerializedObject;

                public SuperRoomsList(SerializedProperty rootProperty, Editor owner) : base(rootProperty, true)
                {
                    ownerEditor = owner;
                    areaSerializedObject = rootProperty.serializedObject;
                    BuildBasicElements();
                    BindProperty(rootProperty);

                    // Header text already bound, ensure it shows "Rooms"
                    header.Bind("Rooms", rootProperty);

                    // Hook drag-and-drop on header foldout for adding existing RoomAssets by dragging them onto the header
                    header.Foldout.RegisterCallback<DragUpdatedEvent>(OnHeaderDragUpdated);
                    header.Foldout.RegisterCallback<DragPerformEvent>(OnHeaderDragPerform);

                    // Add a context menu action for "Add Null Slot" (shift-add replacement in UIElements)
                    header.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
                    {
                        evt.menu.AppendAction("Add Null Slot", a =>
                        {
                            property.serializedObject.Update();
                            property.arraySize++;
                            property.serializedObject.ApplyModifiedProperties();
                            BuildItems();
                            header.UpdateCounter(true);
                        });
                        evt.menu.AppendAction("Create New Room", a => AddButtonPressed());
                    });
                }

                protected override void AddButtonPressed()
                {
                    // Create a new RoomAsset in the Area's folder (match previous behaviour)
                    // Determine folder for new room assets based on Area asset path
                    var areaAsset = areaSerializedObject.targetObject as AreaAsset;
                    string areaAssetPath = AssetDatabase.GetAssetPath(areaAsset);
                    string areaAssetDirectory = System.IO.Path.GetDirectoryName(areaAssetPath);
                    string areaFolderName = areaAsset.name;
                    string areaFolderPath = System.IO.Path.Combine(areaAssetDirectory, areaFolderName);

                    if (!AssetDatabase.IsValidFolder(areaFolderPath))
                        AssetDatabase.CreateFolder(areaAssetDirectory, areaFolderName);

                    // Find a unique name
                    string baseName = "Room";
                    int suffix = 1;
                    string assetName;
                    do
                    {
                        assetName = $"{baseName}{suffix}";
                        suffix++;
                    }
                    while (AssetDatabase.FindAssets(assetName, new[] { areaFolderPath }).Length > 0);

                    // Create the asset
                    RoomAsset newRoom = ScriptableObject.CreateInstance<RoomAsset>();
                    newRoom.name = assetName;
                    string roomAssetPath = System.IO.Path.Combine(areaFolderPath, $"{assetName}.asset");
                    AssetDatabase.CreateAsset(newRoom, roomAssetPath);

                    // Add new element to serialized array and register
                    property.serializedObject.Update();
                    property.arraySize++;
                    int newIndex = property.arraySize - 1;
                    var newRoomSO = new SerializedObject(newRoom);
                    ownerEditor.RegisterRoom(newRoomSO, property.serializedObject, property.GetArrayElementAtIndex(newIndex));
                    Undo.RegisterCreatedObjectUndo(newRoom, "Added New Room");
                    AssetDatabase.SaveAssets();
                    property.serializedObject.ApplyModifiedProperties();

                    BuildItems();
                    header.UpdateCounter(true);
                    Selection.Select(newIndex);
                }

                protected override void RemoveButtonPressed()
                {
                    if (property == null) return;
                    if (CurrentSize == 0) return;

                    if (Selection.Count < 1)
                    {
                        DeletePropertySlotAt(CurrentSize - 1);
                    }
                    else if (Selection.Count == 1)
                    {
                        int idx = Selection.FirstSelected;
                        // If selected element refers to a room asset, ask delete/keep
                        SerializedProperty element = property.GetArrayElementAtIndex(idx);
                        if (element.objectReferenceValue != null)
                        {
                            RoomAsset roomObj = element.objectReferenceValue as RoomAsset;
                            bool shouldDelete = EditorUtility.DisplayDialog(
                                "Remove Room",
                                $"Do you want to delete the RoomAsset '{roomObj.name}' from the project?\n\n" +
                                "Click 'Delete' to remove the asset file, or 'Keep' to just remove the reference.",
                                "Delete",
                                "Keep"
                            );
                            // Unregister from area and optionally delete file
                            var roomSO = new SerializedObject(element.objectReferenceValue);
                            ownerEditor.UnregisterRoom(roomSO, property, idx, true);
                            if (shouldDelete)
                            {
                                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(roomObj));
                                AssetDatabase.SaveAssets();
                            }
                            property.serializedObject.ApplyModifiedProperties();
                        }
                        else
                        {
                            DeletePropertySlotAt(idx);
                        }
                    }
                    else
                    {
                        // multiple selection: remove each selected
                        for (int i = CurrentSize - 1; i >= 0; i--)
                        {
                            if (Selection[i]) DeletePropertySlotAt(i);
                        }
                    }

                    property.serializedObject.ApplyModifiedProperties();
                    BuildItems();
                }

                private void OnHeaderDragUpdated(DragUpdatedEvent ev)
                {
                    if (DragAndDrop.objectReferences.Length == 0) return;
                    bool anyRoom = false;
                    foreach (var o in DragAndDrop.objectReferences)
                        if (o is RoomAsset) { anyRoom = true; break; }

                    if (anyRoom)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        ev.StopPropagation();
                    }
                }

                private void OnHeaderDragPerform(DragPerformEvent ev)
                {
                    if (DragAndDrop.objectReferences.Length == 0) return;

                    property.serializedObject.Update();

                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is RoomAsset roomAsset)
                        {
                            // If assigned to a different area, ask whether to move it
                            if (roomAsset.area != null && roomAsset.area != (AreaAsset)areaSerializedObject.targetObject)
                            {
                                bool res = EditorUtility.DisplayDialog(
                                    "Move Room?",
                                    $"RoomAsset '{roomAsset.name}' is already registered to AreaAsset '{roomAsset.area.name}'.\n" +
                                    "Rooms should not be registered under more than one area.\n" +
                                    "Would you like to move this Room to the new Area?",
                                    "Move", "Cancel"
                                );
                                if (!res) continue;
                                else
                                {
                                    // Unregister from previous area
                                    var prevRoomSO = new SerializedObject(roomAsset);
                                    // Find its area and ask that editor to unregister. We can do a best-effort:
                                    var prevArea = roomAsset.area;
                                    if (prevArea != null)
                                    {
                                        // Remove reference in previous area
                                        var prevAreaSO = new SerializedObject(prevArea);
                                        var roomsProp = prevAreaSO.FindBackingField(nameof(AreaAsset.rooms));
                                        for (int i = 0; i < roomsProp.arraySize; i++)
                                        {
                                            if (roomsProp.GetArrayElementAtIndex(i).objectReferenceValue == roomAsset)
                                            {
                                                prevAreaSO.Update();
                                                prevAreaSO.FindBackingField(nameof(RoomAsset.area)).objectReferenceValue = null;
                                                prevAreaSO.ApplyModifiedProperties();
                                                roomsProp.DeleteArrayElementAtIndex(i);
                                                prevAreaSO.ApplyModifiedProperties();
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            // append to this area's rooms
                            property.arraySize++;
                            var slot = property.GetArrayElementAtIndex(property.arraySize - 1);
                            ownerEditor.RegisterRoom(new SerializedObject(roomAsset), property.serializedObject, slot);
                        }
                    }

                    property.serializedObject.ApplyModifiedProperties();
                    BuildItems();
                    ev.StopPropagation();
                }

                // Create a visual item. The base class will call the SuperRoomsListItem constructor which calls BindProperty()
                //protected override void CreateItemElement(int index)
                //{
                //    // base implementation expects to create the item instances using reflection/known //constructor pattern
                //    var item = (SuperRoomsListItem)Activator.CreateInstance(typeof//(SuperRoomsListItem), new object[] { this, index });
                //    items.Add(item);
                //    collectionBackground.Add(item);
                //}

                // Called from items when their object field is changed to implement the complex registration logic
                internal void HandleItemChange(int index, RoomAsset oldRoom, RoomAsset newRoom)
                {
                    SerializedProperty listProperty = property;
                    var areaAsset = areaSerializedObject.targetObject as AreaAsset;
                    SerializedObject oldRoomS = oldRoom != null ? new SerializedObject(oldRoom) : null;
                    SerializedObject newRoomS = newRoom != null ? new SerializedObject(newRoom) : null;

                    // Gather scenarios
                    bool addAdditional = false;
                    bool deleteOld = false;

                    if (oldRoom == newRoom) return;
                    if (newRoom != null)
                    {
                        if (newRoom != null && areaAsset.rooms.Contains(newRoom) && newRoom != oldRoom && oldRoom != null)
                        {
                            // User replacing old room with an existing room that is elsewhere in this area.
                            // Ask whether to add additional slot or cancel
                            int choice = EditorUtility.DisplayDialogComplex(
                                "Room Already In Area",
                                $"Room '{newRoom.name}' already exists in this area's Rooms list. How would you like to proceed?",
                                "Keep Both (add extra slot)",
                                "Cancel",
                                "Replace existing"
                            );
                            if (choice == 1) return; // Cancel
                            if (choice == 0) addAdditional = true;
                            else
                            {
                                // Replace existing: find that slot and remove it
                                for (int i = 0; i < listProperty.arraySize; i++)
                                    if (listProperty.GetArrayElementAtIndex(i).objectReferenceValue == newRoom)
                                    {
                                        // Clear it
                                        ownerEditor.UnregisterRoom(new SerializedObject(newRoom), listProperty, i, true);
                                        break;
                                    }
                            }
                        }
                    }
                    else // user cleared the slot
                    {
                        int emptyingChoice = EditorUtility.DisplayDialogComplex(
                            "Remove Room?",
                            $"Do you want to remove '{oldRoom.name}' from this area or delete it from the project?",
                            "Remove From Area", "Cancel", "Delete from Project"
                        );
                        if (emptyingChoice == 0)
                        {
                            // just remove from area
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
                            ownerEditor.UnregisterRoom(oldRoomS, listProperty, index);
                            if (deleteOld)
                            {
                                DestroyImmediate(oldRoom, true);
                                AssetDatabase.SaveAssets();
                            }
                        }
                        if (newRoom != null)
                        {
                            if (newRoom.area != null && newRoom.area != areaAsset) ownerEditor.UnregisterRoom(newRoomS);
                            ownerEditor.RegisterRoom(newRoomS, property.serializedObject, listProperty.GetArrayElementAtIndex(index));
                        }
                    }
                    else
                    {
                        listProperty.arraySize++;
                        ownerEditor.RegisterRoom(newRoomS, property.serializedObject, listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1));
                    }

                    listProperty.serializedObject.ApplyModifiedProperties();

                    // Refresh visuals
                    BuildItems();
                }
            }

            // Item element for the Rooms list
            private class SuperRoomsListItem : SuperListItem<SuperRoomsList, SuperRoomsListItem, RoomAsset>
            {
                public SuperRoomsListItem(SuperRoomsList parentList, int Index) : base(parentList, Index) { }

                public override VisualElement Content()
                {
                    // Build a compact row: ObjectField for RoomAsset
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.style.paddingLeft = 2;
                    row.style.paddingRight = 2;
                    row.style.flexGrow = 1;

                    // Create ObjectField for RoomAsset (no label)
                    var objField = new ObjectField()
                    {
                        objectType = typeof(RoomAsset),
                        allowSceneObjects = false,
                        label = ""
                    };
                    objField.style.flexGrow = 1;
                    objField.style.marginLeft = 2;
                    objField.style.marginRight = 2;

                    // Set initial value from property
                    var currentObj = property.objectReferenceValue as RoomAsset;
                    objField.SetValueWithoutNotify(currentObj);

                    // Register change callback
                    objField.RegisterValueChangedCallback(ev =>
                    {
                        // Defer actual handling to parent list to reuse its helper methods
                        parent.HandleItemChange(Index, currentObj, ev.newValue as RoomAsset);
                    });

                    // Add a small ping/select button to the right (optional)
                    var selectButton = new Button(() =>
                    {
                        var ra = property.objectReferenceValue as RoomAsset;
                        if (ra != null) Selection.activeObject = ra;
                    });
                    selectButton.text = "...";
                    selectButton.style.width = 22;
                    selectButton.style.marginLeft = 4;

                    row.Add(objField);
                    row.Add(selectButton);

                    return row;
                }

                protected override void PostContent()
                {
                    base.PostContent();
                    // Use the label area if needed
                    Label = content.Q<Label>(null, "unity-label");
                    ContextMenuTarget = content;
                }
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
                area.Scene = new SceneReference(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scenePath));
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