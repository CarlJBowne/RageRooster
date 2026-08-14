using System.Collections;
using RageRooster.Core;
using UnityEngine;
using UnityEngine.Events;
using static RageRooster.Services;

[RequireComponent(typeof(Collider))]
public class PlayerEnterTrigger : MonoBehaviour
{
    public UnityEvent Event;

    private void OnTriggerEnter(Collider other)
    {
        if (Gameplay.Active && Player.Owns(other)) Event?.Invoke();
    }
}
