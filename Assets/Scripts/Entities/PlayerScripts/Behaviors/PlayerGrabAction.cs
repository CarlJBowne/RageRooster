using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using SLS.StateMachineH;
using RageRooster.Systems.SaveSystem;

public class PlayerGrabAction : PlayerStateBehavior
{
    public UltEvents.UltEvent successReturn;
    public UltEvents.UltEvent failMissedReturn;
    public UltEvents.UltEvent failBlockedReturn;
    public UltEvents.UltEvent altSwitchReturn;

    public void GrabThrowButton()
    {
        if (Player.SignalManager.Locked) return;
        State.Enter();
    }

    public void EndGrabAttempt()
    {
        MeleeTarget systemTarget = TargetingManager.MeleeChannel.CurrentTarget;
        if (systemTarget) Grabbable.Attempt(systemTarget.gameObject, Player.Grabber.Grab, failMissedReturn.Invoke, failBlockedReturn.Invoke);
        else
        {
            GameObject targetObject = null; //(Placeholder, get via Physics check later.)

            Grabbable.Attempt(targetObject, Player.Grabber.Grab, failMissedReturn.Invoke, failBlockedReturn.Invoke);
        }
    }
}
