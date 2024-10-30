using SLS.StateMachineV2;
using System;
using UnityEngine;
using UnityEngine.AI;

public class ChaseEB : StateBehavior
{
    [SerializeField] float speed;
    [SerializeField] Transform target;
    [SerializeField] float destUpdateRate = 2f;
    [SerializeField] float reachDistance;
    [SerializeField] State reachState;

    private NavMeshAgent agent;
    private TrackerEB playerTracker;
    private float destUpdateTimer;
    private float distance => Vector3.Distance(transform.position, target.position);

    public override void OnAwake()
    {
        agent = GetComponentFromMachine<NavMeshAgent>();
        playerTracker = GetComponent<TrackerEB>();
    }

    public override void OnEnter()
    {
        agent.enabled = true;
        agent.speed = speed;
        UpdateDestination();
    }

    public override void OnFixedUpdate()
    {
        if (playerTracker.Distance(false) <= reachDistance)
        {
            ReachTarget();
            return;
        }

        destUpdateTimer += Time.fixedDeltaTime;
        if (destUpdateTimer > destUpdateRate)
        {
            destUpdateTimer %= destUpdateRate;
            UpdateDestination();
        }
    }

    void UpdateDestination() => agent.SetDestination(target.position);
    void ReachTarget()
    {
        TransitionTo(reachState);
        agent.enabled = false;
    }
}