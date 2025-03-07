using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using SLS.StateMachineV3;

public class PlayerGrabAction : PlayerMovementNegater
{
    public bool air;
    
    [HideProperty] public bool wasHeld;
    [HideProperty] public bool success;

    private Grabbable selectedGrabbable;
    private PlayerRanged grabber;


    public override void OnAwake()
    {
        grabber = GetComponentFromMachine<PlayerRanged>();
    }

    public void AttemptGrab(Grabbable attempt, bool held)
    {
        state.TransitionTo();
        if (attempt != null)
        {
            selectedGrabbable = attempt;
            wasHeld = held;
            success = true;
        }
        else
        {
            success = false;
        }
    }

    public void GrabPoint()
    {
        if (!success) return;
        grabber.BeginGrab(selectedGrabbable);
        if (air && !wasHeld && grabber.dropLaunchUpgrade) grabber.BeginThrow(true);
        success = false;
        wasHeld = false;
        selectedGrabbable = null;
    }

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        if(success) base.HorizontalMovement(out resultX, out resultZ);
        else
        {
            resultX = null;
            resultZ = null;
        }
    }

    public override void VerticalMovement(out float? result)
    {
        if(success) base.VerticalMovement(out result);
        else result = null;
    }
}
