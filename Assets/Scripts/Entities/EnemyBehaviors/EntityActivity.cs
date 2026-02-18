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
        currentState = enabled ? States.Default : States.Inactive;
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

        if (value && currentState != States.Default) currentState = States.Default;
        if (!value && currentState == States.Default) currentState = States.Inactive;
    }

    public void Enable() => enabled = true;
    public void Disable() => enabled = false;
    public void Toggle() => enabled = !enabled;


    public enum States
    {
        Inactive = -1,
        Default,
        Stunned,
        Grabbed,
        Thrown,
        RagDoll
    }

    private States currentState;

    public States State
    {
        get => base.enabled ? currentState : States.Inactive;
        set
        {
            currentState = value;
            enabled = currentState switch
            {
                States.Inactive => false,
                States.Default => true,
                States.Stunned => false,
                States.Grabbed => false,
                States.Thrown => false,
                States.RagDoll => false,
                _ => enabled,
            };
        }
    }
}