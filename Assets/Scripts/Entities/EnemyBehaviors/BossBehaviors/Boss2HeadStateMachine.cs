using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;

public class Boss2HeadStateMachine : StateMachine
{

    public State idleState;
    public State guardingState;
    public State knockedState;
    public State attack1State;
    public State attack2State;

    [HideInInspector] public Boss2CentralController controller;
    [HideInInspector] public Animator animator;

    protected override void Initialize()
    {
        animator = GetComponent<Animator>();
        controller = GetComponentInParent<Boss2CentralController>();
    }
}
