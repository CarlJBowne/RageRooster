using System.Collections;
using System.Collections.Generic;
using RageRooster.Core;
using UnityEngine;
using static RageRooster.Services;

[RequireComponent(typeof(Collider))]
public class PlayerEnterTrigger3 : MonoBehaviour
{
    public UltEvents.UltEvent Event;

    private void OnTriggerEnter(Collider other)
    {
        if (Gameplay.Active && Player.Owns(other)) Event?.Invoke();
    }
}