using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerEnterTrigger3 : MonoBehaviour
{
    public UltEvents.UltEvent Event;

    private void OnTriggerEnter(Collider other)
    {
        if (Gameplay.Active && other == Player.Collider) Event?.Invoke();
    }
}