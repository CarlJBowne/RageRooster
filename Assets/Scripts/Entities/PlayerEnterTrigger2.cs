using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PlayerEnterTrigger2 : MonoBehaviour
{
    public UltEvents.UltEvent Event;

    private void OnTriggerEnter(Collider other)
    {
        if(Gameplay.Active && other.gameObject == Gameplay.Player) Event?.Invoke();
    }
}
