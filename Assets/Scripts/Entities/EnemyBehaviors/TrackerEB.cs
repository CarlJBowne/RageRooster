using System;
using UnityEngine;
using SLS.StateMachineV2;
using EditorAttributes;
using UnityEngine.Events;

public class TrackerEB : StateBehavior
{
    public Transform target;

    

    public float autoCheckRate;

    [ToggleGroup("Distance", nameof(range))]
    [SerializeField] public bool updateDistance;
    [HideInInspector] public float range;

    [ToggleGroup("View Cone", nameof(coneWidth))]
    public bool updateDot;
    [HideInInspector] public float coneWidth;

    [ToggleGroup("Line of Sight", nameof(LOSMask))]
    public bool updateLineOfSight;
    [HideInInspector] public LayerMask LOSMask;

    public UnityEvent conditionalEvent;
    [SerializeField] bool exitZone;

    [ToggleGroup("Auto Rotate", nameof(maxRotateDelta))]
    public bool autoRotate;
    [SerializeField, HideInInspector, Tooltip("1 = full second turn, 50 = 1 FixedUpdate turn")] 
    float maxRotateDelta;
     
    private float autoCheckTimer;
    private float distance;
    private float dot;
    private bool lineOfSight;

    public override void OnAwake()
    {
        if (target == null||
            !updateDistance && !updateDot && !updateLineOfSight
            ) Destroy(this); 
    }

    public override void OnEnter()
    {
        Distance(true);
        Dot(true);
        LineOfSight(true);
    }

    public override void OnFixedUpdate()
    {
        if (autoCheckRate < 0) return;
        autoCheckTimer += Time.fixedDeltaTime;
        if(autoCheckTimer >= autoCheckRate)
        {
            autoCheckTimer %= autoCheckRate;
            CheckData();
        }
        if (autoRotate)
        {
            var dir = (target.transform.position - transform.position).XZ();
            transform.eulerAngles = Vector3.RotateTowards(transform.forward, dir, maxRotateDelta * Mathf.PI * Time.fixedDeltaTime, 0).DirToRot();
        }
    }

    private void CheckData()
    {
        float prevDist = distance;
        float prevDot = dot;
        bool prevLOS = lineOfSight;

        if (updateDistance) Distance(true);
        if (updateDot) Dot(true);
        if (updateLineOfSight) LineOfSight(true);

        if ((updateDistance && prevDist <= range != distance <= range) ||
            (updateDot && prevDot <= coneWidth != dot <= coneWidth) ||
            (updateLineOfSight && prevLOS != lineOfSight)
            ) 
            if (EventConditions()) 
                conditionalEvent?.Invoke();
    }

    public float Distance(bool check)
    {
        if (check) distance = Vector3.Distance(transform.position, target.position);
        return distance;
    }
    public float Dot(bool check)
    {
        if (check) dot = (Vector3.Dot(transform.forward, target.position-transform.position) - 1) * -1;
        return dot;
    }
    public bool LineOfSight(bool check)
    {
        if (check)
        {
            Physics.Linecast(transform.position, target.position, out RaycastHit hit, LOSMask);
            lineOfSight = hit.transform == target;
        }
        return lineOfSight;
    }

    public bool EventConditions() 
    {
        bool within = true;
        if (updateDistance && distance > range) within = false;
        if (updateDot && dot > coneWidth) within = false;
        if (updateLineOfSight && !lineOfSight) within = false;

        return within == !exitZone;
    }
    
}