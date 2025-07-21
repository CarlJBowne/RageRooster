using SLS.StateMachineV3;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class WanderEB : StateBehavior
{
    [SerializeField] float speed;
    [SerializeField] float minWalkDistance = 3f;
    [SerializeField] float maxWalkDistance = 5f;
    [SerializeField] float distanceToProceed = 0.5f;

    private NavMeshAgent agent;
    private bool navMeshFailed;

    public override void OnAwake()
    {
        agent = GetComponentFromMachine<NavMeshAgent>();
        navMeshFailed = !agent.isOnNavMesh;
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        FindNextDestination();
    }

    public override void OnFixedUpdate()
    {
        if (navMeshFailed || agent.hasPath) return;
        if (agent.remainingDistance <= agent.stoppingDistance)
            FindNextDestination();
    }
    void FindNextDestination()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 R = Random.insideUnitSphere * maxWalkDistance;
            if (R.magnitude < minWalkDistance) R *= minWalkDistance;

            if (NavMesh.SamplePosition(transform.position + R, out NavMeshHit hit, maxWalkDistance, 1))
            {
                agent.destination = hit.position;
                if (agent.path.status == NavMeshPathStatus.PathComplete)
                    break;
            }
        }
            
    }
}