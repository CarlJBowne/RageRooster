using System;
using System.Collections;
using UnityEngine;

[Tooltip("Manages behaviors meant to happen only when an entity is active.")]
public class EntityActivity : MonoBehaviour
{
    public SLS.StateMachineH.StateMachine slsStateMachine;
    //public Unity.VisualScripting.StateMachine vsStateMachine;
    public Behaviour[] disableComponents;

    public void Awake() => enabled = base.enabled;

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

    public void ResetState()
    {
        if(slsStateMachine != null) slsStateMachine[0].Enter();
        //if(vsStateMachine != null)
        //{
        //    vsStateMachine.enabled = false;
        //    vsStateMachine.enabled = true;
        //}
    }

    public static void Enable(EntityActivity entityActivity)
    {
        if (entityActivity != null) entityActivity.enabled = true;
    }
    public static void Disable(EntityActivity entityActivity)
    {
        if (entityActivity != null) entityActivity.enabled = false;
    }
    public static void SetState(EntityActivity entityActivity, bool value)
    {
        if (entityActivity != null) entityActivity.enabled = value;
    }
}