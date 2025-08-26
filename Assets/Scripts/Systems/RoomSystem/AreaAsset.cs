using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;

using static UnityEditor.Rendering.FilterWindow;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace RageRooster.RoomSystem
{
    [CreateAssetMenu(fileName = "Area", menuName = "ScriptableObjects/Area")]
    public class AreaAsset : ScriptableObject
    {
        //Config Fields
        [field: SerializeField] public string areaDisplayName { get; protected set; } = "INSERT_DISPLAY_NAME";
        [field: SerializeField] public SceneReference landmarkScene { get; protected set; }
        [field: SerializeField] public List<RoomAsset> rooms { get; protected set; } = new();

        //Active Data
        public AreaRoot root { get; protected set; }

        public bool isCurrent { get; protected set; }


        public IEnumerator LoadArea()
        {
            yield return landmarkScene.LoadEnum();
            yield return new WaitUntil(() => root != null);
        }

        public void Connect(AreaRoot root) => this.root = root;






    }

#if UNITY_EDITOR

    [CustomEditor(typeof(AreaAsset))]
    public class AreaAssetEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            roomsList = CreateRoomsList();





            return base.CreateInspectorGUI();
        }

        private ReorderableList roomsList;

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            AreaAsset areaAsset = (AreaAsset)target;

            // draw AreaName and LandmarkScene fields
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(areaAsset.areaDisplayName), backingField: true));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(areaAsset.landmarkScene), backingField: true), true);

            // draw Rooms List as a custom reorderable list
            SerializedProperty roomsProperty = serializedObject.FindProperty("Rooms", backingField: true);

            roomsList.DoLayoutList();







        }

        private ReorderableList CreateRoomsList()
        {
            SerializedProperty roomsProperty = serializedObject.FindProperty(nameof(AreaAsset.rooms), backingField: true);
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



        public void RegisterRoom(SerializedObject room, SerializedObject area, SerializedProperty listSlot)
        {
            room.FindProperty(nameof(RoomAsset.area), backingField: true).objectReferenceValue = area.targetObject;
            room.ApplyModifiedProperties(); // Ensure changes are applied to the SerializedObject  
            listSlot.objectReferenceValue = room.targetObject;
        }
        public void UnregisterRoom(SerializedObject room, SerializedProperty listProperty, int index, bool deleteSlot = false, bool deleteFile = false)
        {
            listProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
            room.FindProperty(nameof(RoomAsset.area), backingField: true).objectReferenceValue = null;
            room.ApplyModifiedProperties();
            if (deleteSlot) listProperty.DeleteArrayElementAtIndex(index);
        }
        public void UnregisterRoom(SerializedObject room)
        {
            SerializedProperty areaProp = room.FindProperty(nameof(RoomAsset.area), backingField: true);
            var area = areaProp.objectReferenceValue as AreaAsset;
            int ID = area.rooms.IndexOf(room.targetObject as RoomAsset);

            area.rooms.Remove(room.targetObject as RoomAsset);
            areaProp.objectReferenceValue = null;
            room.ApplyModifiedProperties();
        }

    }

#endif
}