using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Room", menuName = "ScriptableObjects/Room")]
public class RoomAsset : ScriptableObject
{
    //Serialized Data
    [field: SerializeField] public AreaAsset Area { get; protected set; }
    public SceneReference scene;
    public GameObject landmarkPrefab;


    //Active Data
    public RoomRoot Root { get; protected set; }







}
