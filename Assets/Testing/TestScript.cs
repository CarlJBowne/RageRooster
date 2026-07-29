using EditorAttributes;
using FMOD.Studio;
using RageRooster.World;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SLS.ObjectUtilities;
using FMODUnity;
using RageRooster.Systems;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using SLS.MenuCore;

public class TestScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!Player.IsPlayer(other)) return;

        Enum().Begin(this);
        static IEnumerator Enum()
        {
            Cameras.LockPrimary(true, false);
            yield return WaitFor.Seconds(0.4f);
            Cameras.LockPrimary(true, true);
            Overlay.BetweenUI.DoFadeAlpha(1, 1f);
        }
    }


}
