using System;
using UnityEngine;
using SLS.StateMachineV2;
using UnityEngine.AI;

public class ChaseEB : StateBehavior
{
    [SerializeField] Transform target;
    [SerializeField] float destUpdateRate = 2f;
    [SerializeField] float reachDistance;
    [SerializeField] float loseDistance;
    [SerializeField] State loseState;

    private NavMeshAgent agent;
    private TrackerEB playerTracker;
    private float destUpdateTimer;
    private float distance => Vector3.Distance(transform.position, target.position);

    public override void OnAwake()
    {
        agent = GetComponentFromMachine<NavMeshAgent>();
    }

    public override void OnFixedUpdate()
    {
        if (agent.remainingDistance <= reachDistance)
        {
            ReachTarget();
            return;
        }
            
        destUpdateTimer += Time.fixedDeltaTime;
        if(destUpdateTimer > destUpdateRate)
        {
            destUpdateTimer %= destUpdateRate;
            UpdateDestination();
        }
    }

    void UpdateDestination()
    {
        agent.SetDestination(target.position);
    }
    void ReachTarget()
    {
        TransitionTo(state[0]);
        agent.enabled = false;
    }
}