using System;
using UnityEngine;
using SLS.StateMachineV2;
using EditorAttributes;
using UnityEngine.Events;

public class TrackerEB : StateBehavior
{
    public Transform target;

    public float range;
    public float viewConeWidth;

    public float autoCheckRate;
    public bool updateDistance;
    public bool updateDot;
    public bool updateLineOfSight;
    public LayerMask LOSMask;
    public UnityEvent<bool> WithinDistanceChange;
    public UnityEvent<bool> WithinViewConeChange;
    public UnityEvent<bool> WithinLineOfSightChange;
    public UnityEvent All3Enter;


    private float autoCheckTimer;
    private float distance;
    private float dot;
    private bool lineOfSight;

    public override void OnFixedUpdate()
    {
        if (autoCheckRate < 0) return;
        autoCheckTimer += Time.fixedDeltaTime;
        if(autoCheckTimer >= autoCheckRate)
        {
            autoCheckTimer %= autoCheckRate;
            CheckData();
        }
    }

    private void CheckData()
    {
        if (updateDistance) Distance(true);
        if (updateDot) Dot(true);
        if (updateLineOfSight) LineOfSight(true);

        if(All3Enter != null && distance <= range && dot <= viewConeWidth && lineOfSight) All3Enter?.Invoke();
    }

    public float Distance(bool check)
    {
        float preValue = distance;
        if (check) distance = Vector3.Distance(transform.position, target.position);
        if (preValue <= range != distance <= range) WithinDistanceChange?.Invoke(distance <= range);
        return distance;
    }
    public float Dot(bool check)
    {
        float preValue = dot;
        if (check) dot = Vector3.Dot(transform.forward, target.position-transform.position);
        if (preValue <= viewConeWidth != dot <= viewConeWidth) WithinViewConeChange?.Invoke(distance <= range);
        return dot;
    }
    public bool LineOfSight(bool check)
    {
        bool preValue = lineOfSight;
        if (check)
        {
            Physics.Linecast(transform.position, target.position, out RaycastHit hit, LOSMask);
            lineOfSight = hit.transform == target;
        }
        if(preValue != lineOfSight) WithinLineOfSightChange?.Invoke(lineOfSight);
        return lineOfSight;
    }

}