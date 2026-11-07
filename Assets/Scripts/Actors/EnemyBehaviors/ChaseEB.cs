using SLS.StateMachineH;
using System;
using UnityEngine;
using UnityEngine.AI;

[System.Obsolete]
public class ChaseEB : StateBehavior
{
    [SerializeField] float speed;
    [SerializeField] float destUpdateRate = 2f;
    [SerializeField] float reachDistance;
    [SerializeField] State reachState;

    private NavMeshAgent agent;
    private TrackerEB playerTracker;
    private Timer.Loop destUpdateTimer;

    protected override void OnAwake()
    {
        agent = GetComponentFromMachine<NavMeshAgent>();
        playerTracker = State.Parent.GetComponent<TrackerEB>();
        destUpdateTimer = new(destUpdateRate);
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        agent.enabled = true;
        agent.speed = speed;
        UpdateDestination();
    }

    protected override void OnFixedUpdate()
    {
        if (playerTracker.Distance(false) <= reachDistance)
        {
            playerTracker.PhaseTransition(reachState);
            agent.enabled = false;

            return;
        }

        destUpdateTimer.Tick(UpdateDestination);
    }

    void UpdateDestination() => agent.SetDestination(playerTracker.target.transform.position);

}