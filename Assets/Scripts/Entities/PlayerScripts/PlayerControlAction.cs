using AYellowpaper.SerializedCollections;
using FMOD.Studio;
using SLS.StateMachineV3;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UltEvents;

public class PlayerControlAction : PlayerStateBehavior
{
    public bool lockAction = true;

    public SerializedDictionary<InputActionReference, UltEvent> feedingActions;

    public State nextState;

    public override void OnAwake() => controller.currentAction = this;

    public override void OnEnter(State prev)
    {
        if (lockAction) controller.readyForNextAction = false; 
    }

    public bool IsActionValid(InputActionReference button) => feedingActions.ContainsKey(button);

    public void Finish() => nextState.TransitionTo();

}