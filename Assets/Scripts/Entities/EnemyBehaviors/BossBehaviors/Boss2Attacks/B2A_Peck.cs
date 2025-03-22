using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B2A_Peck : Boss2HeadStateBehavior
{
    public Transform headTargetTransform;
    public Transform playerProxyTransform;
    public Transform animatedTargetTransform;
    public float followPlayerRate;
    public float yPosition;


    public override void OnFixedUpdate()
    {
        Vector3 newPosition = headTargetTransform.position;
        if(followPlayerRate > 0)
            newPosition = Vector3.MoveTowards(headTargetTransform.position.XZ(), playerProxyTransform.position.XZ(), followPlayerRate * Time.deltaTime);

        newPosition.y = yPosition;

        headTargetTransform.position = newPosition;
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        headTargetTransform.position = animatedTargetTransform.position;
    }
}
