using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;

public class PlayerStateAnimator : StateAnimator
{

    public bool isAction = false;
    public bool readyOnEnd = false;
    public State onFinishState;

    private PlayerController controller;

    public override void OnAwake()
    {
        base.OnAwake();
        controller = M.GetComponent<PlayerController>();
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        if (isAction) controller.currentAction = null;
    }
    
    public void Finish() 
    {
        if (readyOnEnd) controller.readyForNextAction = true;
        if (onFinishState != null) onFinishState.TransitionTo();
    }
}
