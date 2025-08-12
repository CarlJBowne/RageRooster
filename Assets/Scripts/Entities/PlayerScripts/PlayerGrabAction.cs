using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using SLS.StateMachineH;

public class PlayerGrabAction : PlayerStateBehavior
{
    public bool air;
    public string animationName;
    
    [HideProperty] public bool success;

    private IGrabbable selectedGrabbable;
    [SerializeField] private PlayerRanged ranged;
    [SerializeField] private PlayerMovementAnimator movementNegator;

    protected override void OnSetup()
    {
        base.OnSetup();
        ranged = GetComponentFromMachine<PlayerRanged>();
        movementNegator = GetComponentFromMachine<PlayerMovementAnimator>();
    }

    public void BeginGrabAttempt(IGrabbable attempt)
    {
        State.Enter();
        Machine.animator.CrossFade(animationName, .1f, -1, 0f);
        if (attempt != null)
        {
            selectedGrabbable = attempt;
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
        if (!success || selectedGrabbable == null)
        {
            IGrabbable lastMinute = PlayerInteracter.Get().HasUsableGrabbable();
            if(lastMinute == null) return;
            selectedGrabbable = lastMinute;
        }
        ranged.GrabPoint(selectedGrabbable);
        if (air && ranged.dropLaunchUpgrade && Input.Grab.IsPressed()) ranged.TryGrabThrowAir(this);
        success = false;
        selectedGrabbable = null;
    }

    public void Finish(State successState, State failState)
    {
        (ranged.currentGrabbed != null ? successState : failState).Enter();
        Machine.animator.CrossFade("GroundBasic", .1f);
    }
}
