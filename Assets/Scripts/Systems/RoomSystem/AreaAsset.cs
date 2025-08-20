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

[CreateAssetMenu(fileName = "Area", menuName = "ScriptableObjects/Area")]
public class AreaAsset : ScriptableObject
{
    //Config Fields
    [field: SerializeField] public string areaDisplayName { get; protected set; } = "INSERT_DISPLAY_NAME";
    [field: SerializeField] public SceneReference landmarkScene { get; protected set; }
    [field: SerializeField] public List<RoomAsset> rooms { get; protected set; } = new List<RoomAsset>();

    //Active Data
    public AreaRoot root { get; protected set; }

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

        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = roomsList.serializedProperty.GetArrayElementAtIndex(index);
            RoomAsset oldRoom = element.objectReferenceValue as RoomAsset;
            AreaAsset This = (AreaAsset)target;

            EditorGUI.BeginChangeCheck();
            RoomAsset newRoom = (RoomAsset)EditorGUI.ObjectField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                oldRoom,
                typeof(RoomAsset),
                false
            );
            if (EditorGUI.EndChangeCheck()) ListOperation(oldRoom, newRoom, This, element);
        };


        list.elementHeightCallback = (int index) =>
        {
            return EditorGUIUtility.singleLineHeight; // Adjust height as needed
        };

        list.onAddCallback = (ReorderableList l) =>
        {
            // Generate a unique name for the new RoomAsset
            string baseName = "Room";
            int suffix = 1;
            string assetName;
            string areaAssetPath = AssetDatabase.GetAssetPath(target);
            string areaAssetDirectory = System.IO.Path.GetDirectoryName(areaAssetPath);
            string areaFolderName = ((AreaAsset)target).name;
            string areaFolderPath = System.IO.Path.Combine(areaAssetDirectory, areaFolderName);

            // Ensure the folder exists
            if (!AssetDatabase.IsValidFolder(areaFolderPath))
            {
                AssetDatabase.CreateFolder(areaAssetDirectory, areaFolderName);
            }

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
            Undo.RegisterCreatedObjectUndo(newRoom, "Added New Object");
            AssetDatabase.SaveAssets();
            roomsProperty.arraySize++;
            roomsProperty.GetArrayElementAtIndex(roomsProperty.arraySize - 1).objectReferenceValue = newRoom;
            newRoom._SetArea((AreaAsset)target); // Set the area reference
            serializedObject.ApplyModifiedProperties();
        };

        list.onRemoveCallback = (ReorderableList l) =>
        {
            list.onRemoveCallback = (ReorderableList l) =>
            {
                if (roomsProperty.arraySize == 0) return;
                int index = l.index;
                if (index < 0 || index >= roomsProperty.arraySize) return;
                SerializedProperty element = roomsProperty.GetArrayElementAtIndex(index);
                Object roomObj = element.objectReferenceValue;
                if (roomObj != null)
                {
                    string roomPath = AssetDatabase.GetAssetPath(roomObj);
                    bool shouldDelete = EditorUtility.DisplayDialog(
                        "Remove Room",
                        $"Do you want to delete the RoomAsset '{roomObj.name}' from the project?\n\n" +
                        "Click 'Delete' to remove the asset file, or 'Keep' to just remove the reference.",
                        "Delete",
                        "Keep"
                    );
                    if (shouldDelete)
                    {
                        AssetDatabase.DeleteAsset(roomPath);
                    }
                }
                // Remove the element from the list
                element.FindProperty(nameof(RoomAsset.area), backingField: true).objectReferenceValue = null; // Clear the area reference
                roomsProperty.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
            };
        };

        return list;
    }

    void ListOperation(RoomAsset oldRoom, RoomAsset newRoom, AreaAsset This, SerializedProperty element)
    {
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
            if(oldRoom != null) //Slot had another room in it.
            {
                int res = EditorUtility.DisplayDialogComplex(
                    "Replace Room?",
                    $"Do you want to replace '{oldRoom.name}' with '{newRoom.name}'?",
                    "Replace", "Cancel", "Add in new slot instead"
                );
                if(res == 0)
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
            if(oldRoom != null)
            {
                //oldRoom.area.rooms.Remove(oldRoom);
                oldRoom._SetArea(null);
                if (deleteOld)
                {
                    DestroyImmediate(oldRoom, true);
                    AssetDatabase.SaveAssets();
                }
            }
            element.objectReferenceValue = newRoom;
            if (newRoom != null)
            {
                if(newRoom.area != null && newRoom.area != This)
                {
                    newRoom.area.rooms.Remove(newRoom);
                    newRoom._SetArea(null);
                }
                newRoom._SetArea(This);
            }
        }
        else
        {
            roomsList.serializedProperty.arraySize++;
            roomsList.serializedProperty.GetArrayElementAtIndex(roomsList.serializedProperty.arraySize - 1).objectReferenceValue = newRoom;
            newRoom._SetArea(This);
        }

        roomsList.serializedProperty.serializedObject.ApplyModifiedProperties();
    }

}

#endif