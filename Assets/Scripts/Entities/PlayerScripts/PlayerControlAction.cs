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

    public override void OnAwake()
    {
        state.onActivatedEvent.AddListener(Begin);
    }

    void Begin(State prev)
    {
        controller.currentAction = this;
        if (lockAction) controller.readyForNextAction = false;
    }

    public bool IsActionValid(InputActionReference button)
    {
        Debug.Log($"They are {button == feedingActions.Keys.ToList()[0]}");
        return feedingActions.ContainsKey(button);
    }

    public InputActionReference AAAAAAAAAAAAA;

    public void Finish() => nextState.TransitionTo();

}