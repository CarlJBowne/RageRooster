using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "Room", menuName = "ScriptableObjects/Room")]
public class RoomAsset : ScriptableObject
{
    //Serialized Data
    [field: SerializeField] public string roomDisplayName { get; protected set; } = "INSERT_DISPLAY_NAME";
    [field: SerializeField] public AreaAsset area { get; protected set; }
    [field: SerializeField] public SceneReference scene { get; protected set; }
    [field: SerializeField] public Prefab adjacentLOD { get; protected set; }


    //Active Data
    public RoomRoot root { get; protected set; }
    public GameObject adjacentLODInstance { get; protected set; }








#if UNITY_EDITOR
    internal void _AreaSet_EditorOnly(AreaAsset newArea) => area = newArea;
#endif
}

#if UNITY_EDITOR

[CustomEditor(typeof(RoomAsset))]
public class RoomAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
#if true

        base.OnInspectorGUI();
#else
        RoomAsset roomAsset = (RoomAsset)target;
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.roomDisplayName), backingField: true));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.scene), backingField: true));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoomAsset.adjacentLOD), backingField: true));
#endif
    }
}

#endif