using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class PlayerMovementAnimatorDisabler : PlayerStateBehavior
{
    public enum ConditionalCheck
    {
        DamagableInFrontOf,
        GrabbableInFrontOf
    }
    public ConditionalCheck check;
    public bool checkOnEnter = true;

    [SerializeField] private PlayerMovementAnimator animator;

    protected override void OnSetup()
    {
        base.OnSetup();
        if(animator == null) animator = Machine.GetComponent<PlayerMovementAnimator>();
        if(animator == null) animator = Machine.GetComponent<PlayerMovementAnimator>();
    }

    public bool Check()
    {
        bool result = check switch
        {
            ConditionalCheck.DamagableInFrontOf => DamagableInFrontOf(),
            ConditionalCheck.GrabbableInFrontOf => GrabbableInFrontOf(),
            _ => false,
        };
        animator.locked = !result;
        return result;
    }

    public bool DamagableInFrontOf() => playerMovementBody.CheckForTypeInFront<IDamagable>() != null;

    public bool GrabbableInFrontOf() => playerMovementBody.CheckForTypeInFront<Grabbable>() != null;

    protected override void OnEnter(State prev, bool isFinal)
    {
        animator.locked = true;
        if (checkOnEnter) Check();
    }
    protected override void OnExit(State next)
    {
        animator.locked = false;
    }

}
