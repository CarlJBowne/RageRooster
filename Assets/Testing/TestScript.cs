using EditorAttributes;
using FMOD.Studio;
using RageRooster.RoomSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RageRooster.Systems.ObjectPooling;
using FMODUnity;
using RageRooster.Systems;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

public class TestScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!Player.IsPlayer(other)) return;
        Cameras.LockPrimary(true, false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Player.IsPlayer(other)) return;
        Cameras.LockPrimary(false, false);

    }
}
