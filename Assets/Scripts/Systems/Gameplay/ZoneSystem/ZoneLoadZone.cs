using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneLoadZone : MonoBehaviour
{
    public ZoneTransition transition;

    public bool isWithin;

    private void OnDisable() => Gameplay.onPlayerRespawn -= PlayerDisabled;

    private void OnTriggerEnter(Collider other)
    {
        if (!Gameplay.Active || other.gameObject != Gameplay.Player) return;
        isWithin = true;
        Gameplay.onPlayerRespawn += PlayerDisabled;
    }
    private void OnTriggerExit(Collider other)
    {
        if (!Gameplay.Active || other.gameObject != Gameplay.Player) return;
        isWithin = false;
        Gameplay.onPlayerRespawn -= PlayerDisabled;
    }

    void PlayerDisabled()
    {
        isWithin = false;
        Gameplay.onPlayerRespawn -= PlayerDisabled;
    }
}
