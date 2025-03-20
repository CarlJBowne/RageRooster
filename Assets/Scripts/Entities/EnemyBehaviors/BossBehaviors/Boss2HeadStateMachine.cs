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

    protected override void Initialize()
    {
        controller = GetComponentInParent<Boss2CentralController>();
    }
}
