using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RageRooster.Services;

public class VolcanicVent : MonoBehaviour
{
    public float glideHeight;
    public float hellcopterTargetHeight;
    public float hellcopterSpeed;

    private void OnTriggerEnter(Collider other)
    {
        if(Player.Owns(other))
            Player.CurrentVent = this;
    }
    private void OnTriggerExit(Collider other)
    {
        if (Player.Owns(other) && Player.CurrentVent == this)
            Player.CurrentVent = null;
    }
}
