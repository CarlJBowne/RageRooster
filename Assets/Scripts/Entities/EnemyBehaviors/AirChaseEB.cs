using SLS.StateMachineV2;
using System;
using UnityEngine;
using UnityEngine.AI;

public class AirChaseEB : StateBehavior
{
    [SerializeField] float speed;
    [SerializeField] float reachDistance;
    [SerializeField] State reachState;

    private TrackerEB playerTracker;


    public override void OnEnter()
    {
        playerTracker = state.parent.GetComponent<TrackerEB>();
    }

    public override void OnFixedUpdate()
    {
        if (playerTracker.Distance(true) <= reachDistance)
        {
            playerTracker.PhaseTransition(reachState);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, playerTracker.target.position, speed * Time.fixedDeltaTime);
    }

}