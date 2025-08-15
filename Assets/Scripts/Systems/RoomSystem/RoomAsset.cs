using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Room", menuName = "ScriptableObjects/Room")]
public class RoomAsset : ScriptableObject
{
    //Serialized Data
    [field: SerializeField] public AreaAsset area { get; protected set; }
    [field: SerializeField] public SceneReference scene { get; protected set; }
    [field: SerializeField] public Prefab adjacentLOD { get; protected set; }


    //Active Data
    public RoomRoot root { get; protected set; }
    public GameObject adjacentLODInstance { get; protected set; }






}
