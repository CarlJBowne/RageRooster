using System.Collections;
using System.Collections.Generic;
using SLS.StateMachineH;
using UnityEngine;
using UnityEngine.Events;

// This behavior is made to serve as a countdown towards activating an action.
// The enemy will remain idle while the countdown happens.

public class CountdownEB : StateBehavior
{
    // Timer used for countdown.
    public Timer.OneTime timer = new(5f);
    // Event that is called when countdown is over. Set in editor.
    public UnityEvent countdownEvent;

    // Begin timer when state is entered.
    protected override void OnEnter(State prev, bool isFinal)
    {
        timer.Begin();
    }

    // Tick timer on update, call ActivateEvent when timer is run down.
    protected override void OnUpdate()
    {
        timer.Tick(ActivateEvent);
    }

    // Activates the UnityEvent specified in the editor.
    public void ActivateEvent()
    {
        countdownEvent.Invoke();
    }
}
