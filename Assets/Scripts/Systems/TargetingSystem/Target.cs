using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Target : MonoBehaviour
{

    public float GetDistance(TargetingRange range) => Vector3.Distance(range.front.position, transform.position);

    public float GetAngle(TargetingRange range) => Vector3.Angle(range.front.forward, transform.position - range.front.position);

    protected virtual void OnEnable()
    {
        TargetingManager.AddActiveTarget(this);
        currentState = TargetStates.OutOfRange;
    }
    protected virtual void OnDisable()
    {
        TargetingManager.RemoveActiveTarget(this);
        currentState = TargetStates.Inactive;
    }

    public enum TargetStates
    {
        Inactive,
        OutOfRange,
        WithinRange,
        Targeted
    }
    public virtual TargetStates TargetState
    {
        get => currentState;
        set
        {
            if (currentState == value) return;
            if (currentState == TargetStates.Inactive || value == TargetStates.Inactive) return;

            //Do visual effects here.

            currentState = value;
        }
    }
    protected TargetStates currentState;


}