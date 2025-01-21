using System;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete]
public class CheckpointZone : MonoBehaviour
{
    public Vector3 targetPositionRelative;

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out PlayerHealth hp))
        {
            //hp.SetRespawnPoint(targetPositionRelative + transform.position);
        }
    }
}