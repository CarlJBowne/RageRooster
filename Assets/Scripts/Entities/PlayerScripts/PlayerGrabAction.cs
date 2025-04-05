using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using SLS.StateMachineV3;

public class PlayerGrabAction : PlayerStateBehavior
{
    public bool air;
    
    [HideProperty] public bool wasHeld;
    [HideProperty] public bool success;

    private Grabbable selectedGrabbable;
    private PlayerRanged grabber;
    private PlayerMovementAnimator movementNegator;

    public override void OnAwake()
    {
        grabber = GetComponentFromMachine<PlayerRanged>();
        movementNegator = GetComponent<PlayerMovementAnimator>();
        movementNegator = GetComponent<PlayerMovementAnimator>();
    }

    public void AttemptGrab(Grabbable attempt, bool held)
    {
        state.TransitionTo();
        if (attempt != null)
        {
            selectedGrabbable = attempt;
            wasHeld = held;
            success = true;
            movementNegator.locked = false;
        }
        else
        {
            success = false;
            movementNegator.locked = true;
        }
    }

    public void GrabPoint()
    {
        if (!success || selectedGrabbable == null) return;
        grabber.BeginGrab(selectedGrabbable);
        if (air && grabber.dropLaunchUpgrade && !Input.Grab.IsPressed()) grabber.BeginThrow(true);
        success = false;
        wasHeld = false;
        selectedGrabbable = null;
    }


}
