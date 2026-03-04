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
        TargetType.Melee systemTarget = TargetingManager.MeleeChannel.CurrentTargetType<TargetType.Melee>();
        if (systemTarget) Grabbable.Attempt(systemTarget.This.gameObject, Succeed, failMissedReturn.Invoke, failBlockedReturn.Invoke);
        else
        {
            GameObject targetObject = null; //(Placeholder, get via Physics check later.)

            //Grabbable.Attempt(targetObject, Player.Grabber.Grab, failMissedReturn.Invoke, failBlockedReturn.Invoke);
        }
    }

    void Succeed(Grabbable G)
    {
        Player.Grabber.Grab(G);
        successReturn?.Invoke();
    }
}
