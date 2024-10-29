using System;
using System.Linq;
using UnityEngine;
using SLS.StateMachineV2;
using UnityEngine.AI;

public class PatrolEB : StateBehavior
{
    private NavMeshAgent agent;
    private TrackerEB playerTracker;

    [SerializeField] Transform[] path;
    [SerializeField] float distanceToProceed = 0.5f;

    int currentDestination;

    public override void OnAwake()
    {
        agent = GetComponentFromMachine<NavMeshAgent>();
    }

    public override void OnEnter()
    {
        int targetPath = 0;
        if(currentDestination == -1)
        {
            targetPath = 
                Enumerable.Range(0, path.Length)
                .OrderBy(i => Vector3.Distance(transform.position, path[i].position))
                .First();

        }
        SetNextDest(targetPath);
        currentDestination = targetPath;
    }

    public override void OnFixedUpdate()
    {
        if(agent.remainingDistance < distanceToProceed) SetNextDest(++currentDestination);
    }
    void SetNextDest(int i)
    {
        if (i == path.Length) i = 0;
        agent.SetDestination(path[i].position);
    }

    public override void OnExit()
    {
        currentDestination = -1;
    }

}