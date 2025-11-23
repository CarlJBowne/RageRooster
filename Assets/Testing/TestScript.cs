using EditorAttributes;
using FMOD.Studio;
using RageRooster.RoomSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RageRooster.Systems.ObjectPool;
using FMODUnity;
using RageRooster.Systems;

public class TestScript : MonoBehaviour
{
    public EventReference secondMusic;

    private void OnTriggerEnter(Collider other)
    {
        if(other == Player.Collider)
        {
            Music.BeginSecondaryMusic(secondMusic);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other == Player.Collider)
        {
            Music.ReturnToPrimaryMusic();
        }
    }
}
