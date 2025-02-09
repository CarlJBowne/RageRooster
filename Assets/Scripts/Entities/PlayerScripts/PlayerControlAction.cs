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
using EditorAttributes;
using Timer;
using UnityEditor;

public class PlayerControlAction : PlayerStateBehavior
{
    public bool lockAction = true;

    public ActionEntry[] possibleActions;

    [System.Serializable]
    public struct ActionEntry
    {
        public InputActionReference input;
        public UltEvent result;
    }

    public State nextState;
    public override void OnAwake() => state.onActivatedEvent.AddListener(Begin);

    void Begin(State prev)
    {
        controller.currentAction = this;
        if (lockAction) controller.readyForNextAction = false;
    }

    public bool HasAction(InputActionReference button)
    {
        for (int i = 0; i < possibleActions.Length; i++) 
            if (possibleActions[i].input.action == button.action) 
                return true;
        return false;

        //return feedingActions.ContainsKey(button);
    }

    public bool TryNextAction(InputActionReference button)
    {
        for (int i = 0; i < possibleActions.Length; i++)
        {
            if (possibleActions[i].input.action.ToString() == button.action.ToString())
            {
                possibleActions[i].result?.Invoke();
                return true;
            }
        }
        return false;
    }

    public void Finish() => nextState.TransitionTo();

}