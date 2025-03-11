using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolcanicVent : MonoBehaviour
{
    public float glideHeight;
    public float hellcopterTargetHeight;
    public float hellcopterSpeed;

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out PlayerMovementBody player)) 
            player.currentVent = this;
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovementBody player) && player.currentVent == this) 
            player.currentVent = null;
    }
}
