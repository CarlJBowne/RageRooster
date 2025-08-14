using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Area", menuName = "ScriptableObjects/Area")]
public class AreaAsset : ScriptableObject
{
    public AreaRoot Root { get; protected set; }

    [field: SerializeField] public List<RoomAsset> Rooms { get; protected set; } = new List<RoomAsset>();
}
