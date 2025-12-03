using System;
using System.Collections;
using UnityEngine;

[Tooltip("Manages behaviors meant to happen only when an entity is active.")]
public class EntityActivity : MonoBehaviour
{
    public Behaviour[] disableComponents;

    public void Awake()
    {
        enabled = base.enabled;
        currentState = enabled ? State.Default : State.Inactive;
    }

    private void OnEnable() => EnabledSet(true);
    private void OnDisable() => EnabledSet(false);

    public void EnabledSet(bool value)
    {
        base.enabled = value;

        if (!Application.isPlaying) return;

        if (disableComponents.Length > 0)
            for (int i = 0; i < disableComponents.Length; i++)
                if (disableComponents[i] != null)
                    disableComponents[i].enabled = value;
    }

    public void Enable() => enabled = true;
    public void Disable() => enabled = false;
    public void Toggle() => enabled = !enabled;


    public enum State
    {
        Inactive = -1,
        Default,
        Stunned,
        Grabbed,
        Thrown,
        RagDoll
    }

    private State currentState;

    public State CurrentState
    {
        get => base.enabled ? currentState : State.Inactive;
        set
        {
            currentState = value;
            enabled = currentState switch
            {
                State.Inactive => false,
                State.Default => true,
                State.Stunned => false,
                State.Grabbed => false,
                State.Thrown => false,
                State.RagDoll => false,
                _ => enabled,
            };
        }
    }
}