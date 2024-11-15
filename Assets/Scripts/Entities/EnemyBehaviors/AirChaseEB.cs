using SLS.StateMachineV2;
using System;
using UnityEngine;
using UnityEngine.AI;

public class AirChaseEB : StateBehavior
{
    [SerializeField] float speed;
    [SerializeField] Transform target;
    [SerializeField] float reachDistance;
    [SerializeField] State reachState;

    private TrackerEB playerTracker;


    public override void OnEnter()
    {
        playerTracker = GetComponent<TrackerEB>();
    }

    public override void OnFixedUpdate()
    {
        if (playerTracker.Distance(true) <= reachDistance)
        {
            ReachTarget();
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.fixedDeltaTime);
    }

    void ReachTarget()
    {
        TransitionTo(reachState);
    }
}