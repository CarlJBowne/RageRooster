using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Was going to just be a debug script, but ended up being used as a timer that destroys whatever it is attached to when run down.

public class TimedEvent : MonoBehaviour
{
    public Timer.OneTime timer = new(1f);
    public UltEvents.UltEvent Action;

    public void Start() => timer.Begin();
    public void Update() => timer.Tick(Action.Invoke);

    public void RemoveExplosion()
    {
        Destroy(gameObject);
    }
}
