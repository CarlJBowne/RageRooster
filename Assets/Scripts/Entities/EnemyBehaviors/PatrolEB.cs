using SLS.StateMachineV2;
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

    public override void OnAwake() => agent = GetComponentFromMachine<NavMeshAgent>();

    public override void OnEnter()
    {
        agent.enabled = true;
        agent.speed = speed;

        int targetPath = Enumerable.Range(0, path.Length)
                .OrderBy(i => Vector3.Distance(transform.position, path[i]))
                .First();
        SetNextDest(targetPath);
        currentDestination = targetPath;
    }

    public override void OnFixedUpdate()
    {
        if (agent.remainingDistance < distanceToProceed) SetNextDest(++currentDestination);
    }
    void SetNextDest(int i)
    {
        if (i == path.Length) i = 0;
        agent.SetDestination(path[i]);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red + Color.yellow;
        Gizmos.DrawLineStrip(path, true);
    }
}