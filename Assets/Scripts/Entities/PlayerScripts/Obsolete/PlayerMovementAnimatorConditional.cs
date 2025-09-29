using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementAnimatorConditional : PlayerMovementAnimator
{
    public enum ConditionalCheck
    {
        DamagableInFrontOf,
        GrabbableInFrontOf
    }
    public ConditionalCheck check;
    public bool checkOnEnter = true;

    private bool defaultState;

    public bool Check()
    {
        bool result = check switch
        {
            ConditionalCheck.DamagableInFrontOf => DamagableInFrontOf(),
            ConditionalCheck.GrabbableInFrontOf => GrabbableInFrontOf(),
            _ => false,
        };
        locked = !result;
        return result;
    }

    public bool DamagableInFrontOf() => playerMovementBody.CheckForTypeInFront<IDamagable>() != null;

    public bool GrabbableInFrontOf() => playerMovementBody.CheckForTypeInFront<Grabbable>() != null;

    protected override void OnAwake() => defaultState = locked;

    protected override void OnEnter(State prev, bool isFinal)
    {
        locked = defaultState;
        if (checkOnEnter) Check();
    }
    protected override void OnExit(State next)
    {
        locked = !defaultState;
    }

    public override void RunTransfer() => MiscHelperMethods.PlayerMovementAnimatorTransferToRoots.Conditional(this);

}
