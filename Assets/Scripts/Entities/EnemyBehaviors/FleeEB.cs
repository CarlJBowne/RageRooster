using SLS.StateMachineH;
using System;
using UnityEngine;
using UnityEngine.AI;

// Based on ChaseEB. Enemy will back up instead of move forward.

public class FleeEB : StateBehavior
{
    [SerializeField] float speed;
    [SerializeField] float destUpdateRate = 2f;
    [SerializeField] float reachDistance;
    [SerializeField] State reachState;

    private NavMeshAgent agent;
    private TrackerEB playerTracker;
    private Timer_Old destUpdateTimer;

    protected override void OnAwake()
    {
        agent = GetComponentFromMachine<NavMeshAgent>();
        playerTracker = State.Parent.GetComponent<TrackerEB>();
        destUpdateTimer = new(destUpdateRate, UpdateDestination);
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        agent.enabled = true;
        agent.speed = speed;
        UpdateDestination();
    }

    // Here, the enemy's phase transition happens when distance is *greater than* or equal to reach distance
    protected override void OnFixedUpdate()
    {
        if (playerTracker.Distance(false) >= reachDistance)
        {
            playerTracker.PhaseTransition(reachState);
            agent.enabled = false;

            return;
        }

        destUpdateTimer += Time.fixedDeltaTime;
    }

    // Here, a flee point is calculated by adding the difference between the player's position and the enemy's position to the enemy's position.
    void UpdateDestination()
    {
        Vector3 playerPosition = playerTracker.target.transform.position;
        Vector3 currentPosition = agent.transform.position;
        Vector3 fleePoint = currentPosition + (currentPosition - playerPosition);
        agent.SetDestination(fleePoint);
    } 

}
