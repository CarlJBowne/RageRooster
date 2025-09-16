using EditorAttributes;
using RageRooster.RoomSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    //Make private but visible later.
    public int ID;
    public bool rotate;

    [SerializeField, HideInInspector] internal RoomRoot root;

#if UNITY_EDITOR
    [Button("Play from here.")]
    public void BeginFromHere()
    {
        EditorState.EditorDestination = new() 
        {
            area = root.asset.area,
            room = root.asset,
            spawn = this,
            spawnID = ID
        };
        UnityEditor.EditorApplication.isPlaying = true;
    }
#endif
    public void SpawnPlayerAt()
    {
        Player.InstantMove(transform.position, rotate ? transform.eulerAngles.y : null);
    }
}
