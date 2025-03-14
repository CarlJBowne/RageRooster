using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldChangeListenEvent : MonoBehaviour
{
    public WorldChange change;
    public UltEvents.UltEvent resultEvent;

    private void Awake()
    {
        if (change == null || resultEvent == null) return;
        if (change.Enabled) resultEvent?.Invoke();
        else change.Action += resultEvent.Invoke;
    }

    private void OnDestroy()
    {
        if(change != null) change.Action -= resultEvent.Invoke;
    }
}
