using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using EditorAttributes;

[System.Obsolete]
public class PlayerStateAnimator : StateAnimator
{

    public bool isAction = false;
    public bool readyOnEnd = false;
    public State onFinishState;

    private PlayerController controller;

    protected override void OnAwake()
    {
        base.OnAwake();
        controller = Machine.GetComponent<PlayerController>();
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        //if (isAction) controller.currentAction = null;
    }
    
    public void Finish() 
    {
        //if (readyOnEnd) controller.readyForNextAction = true;
        if (onFinishState != null) onFinishState.Enter();
    }

    [Button]
    private void Move()
    {
        State iState = GetComponent<State>();
        //iState.lockReady = isAction;
        //if (onFinishState != null)
        //{
        //    iState.signals.Add("Finish", new());
        //    D next = onFinishState.TransitionTo;
        //    iState.signals["Finish"].AddPersistentCall(next);
        //}
        //
        //StateAnimator newAnim = iState.gameObject.AddComponent<StateAnimator>();
        //newAnim.onEntry = onEntry;
        //newAnim.onEnterName = onEnterName;
        //newAnim.onEnterTime = onEnterTime;


    }
    private delegate void D();
}
