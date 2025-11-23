using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class RandomNavMeshNode : Unit
{
    protected override void Definition()
    {
        Begin = ControlInput(nameof(Begin), FindNextDestination);
        After = ControlOutput(nameof(After));
        maxWalkDistance = ValueInput<float>(nameof(maxWalkDistance), 15);
        minWalkDistance = ValueInput<float>(nameof(minWalkDistance), 5);
        transform = ValueInput<Transform>(nameof(transform), null);
        agent = ValueInput<NavMeshAgent>(nameof(agent), null);
    }

    [PortLabelHidden, DoNotSerialize] public ControlInput Begin;
    [PortLabelHidden, DoNotSerialize] public ControlOutput After;
    [DoNotSerialize] public ValueInput maxWalkDistance;
    [DoNotSerialize] public ValueInput minWalkDistance;
    [DoNotSerialize] public ValueInput transform;
    [DoNotSerialize] public ValueInput agent;

    ControlOutput FindNextDestination(Flow flow)
    {
        float maxWalkDistance = flow.GetValue<float>(this.maxWalkDistance);
        float minWalkDistance = flow.GetValue<float>(this.minWalkDistance);
        Transform transform = flow.GetValue<Transform>(this.transform);
        NavMeshAgent agent = flow.GetValue<NavMeshAgent>(this.agent);

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
        return After;
    }


}