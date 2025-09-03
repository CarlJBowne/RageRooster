using EditorAttributes;
using RageRooster.RoomSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    //Make private but visible later.
    public int ID;

    [SerializeField, HideInInspector] internal RoomRoot root;

#if UNITY_EDITOR
    [Button("Play from here.")]
    public void BeginFromHere()
    {
        EditorState.EditorDestination = new RoomDestination(this);
        UnityEditor.EditorApplication.isPlaying = true;
    }
#endif
    public void SpawnPlayerAt()
    {
        //PlayerStateMachine.Get().InstantMove(this);
    }
}
