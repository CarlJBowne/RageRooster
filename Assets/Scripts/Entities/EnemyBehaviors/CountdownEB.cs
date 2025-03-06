using System.Collections;
using System.Collections.Generic;
using SLS.StateMachineV3;
using UnityEngine;
using UnityEngine.Events;

public class CountdownEB : StateBehavior
{
    public Timer.OneTime timer = new(5f);
    public UnityEvent countdownEvent;

    public override void OnEnter(State prev, bool isFinal)
    {
        timer.Begin();
    }

    public override void OnUpdate()
    {
        timer.Tick(ActivateEvent);
    }

    public void ActivateEvent()
    {
        countdownEvent.Invoke();
    }
}
