using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PlayerEnterTrigger : MonoBehaviour
{
    public UnityEvent Event;

    private void OnTriggerEnter(Collider other)
    {
        if(Gameplay.Active && other.gameObject == Gameplay.Player) Event?.Invoke();
    }
}
