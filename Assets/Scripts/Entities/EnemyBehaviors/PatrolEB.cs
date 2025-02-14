using SLS.StateMachineV3;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class PatrolEB : StateBehavior
{
    [SerializeField] float speed;
    [SerializeField] Vector3[] path;
    [SerializeField] float distanceToProceed = 0.5f;

    private NavMeshAgent agent;
    int currentDestination = 0;
    private bool navMeshFailed;

    public override void OnAwake()
    {
        agent = GetComponentFromMachine<NavMeshAgent>();
        navMeshFailed = !agent.isOnNavMesh;
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        agent.enabled = true;
        agent.speed = speed;

        int targetPath = Enumerable.Range(0, path.Length)
                .OrderBy(i => Vector3.Distance(transform.position, path[i]))
                .First();
        SetNextDest(ref targetPath);
        currentDestination = targetPath;
    }

    public override void OnFixedUpdate()
    {
        if (navMeshFailed) return;
        if (agent.remainingDistance < distanceToProceed)
        {
            currentDestination++;
            SetNextDest(ref currentDestination);
        }
    }
    void SetNextDest(ref int i)
    {
        if (i >= path.Length) i = 0;
        agent.SetDestination(path[i]);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red + Color.yellow;
        Gizmos.DrawLineStrip(path, true);
    }
}