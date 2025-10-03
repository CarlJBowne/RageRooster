using EditorAttributes;
using RageRooster.RoomSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    //Make private but visible later.
    public int ID = -1;
    public bool rotate = true;
    public bool snapToFloor = true;

    [SerializeField, HideInInspector] internal RoomRoot root;

#if UNITY_EDITOR
    [Button("Play from here.")]
    public void BeginFromHere()
    {
        EditorState.EditorDestination = new() 
        {
            room = root.asset,
            spawnID = ID
        };
        UnityEditor.EditorApplication.isPlaying = true;
    }
#endif
    public void SpawnPlayerAt()
    {
        //Cast downwards and get point.
        Vector3 target = transform.position;
        if (snapToFloor && Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit)) target = hit.point;

        Player.InstantMove(target, rotate ? transform.eulerAngles.y : null);
        //Player.MovementBody.InstantSnapToFloor();
    }

    public Destination GetDestination() => new()
    {
        room = root.asset,
        spawnID = ID
    };
}
