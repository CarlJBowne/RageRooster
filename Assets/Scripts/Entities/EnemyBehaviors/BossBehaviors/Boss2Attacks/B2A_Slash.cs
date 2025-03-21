using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B2A_Slash : StateBehavior
{
    public float xMin;
    public float xMax;
    public float zMin;
    public float zMax;

    public float xPercentage;
    public float zPercentage;
    public float yPos;

    public float headMovementMaxDelta;
    public float zMaxMaxDelta;

    public Transform playerProxy;
    public Transform calculatedHeadTarget;

    public override void OnFixedUpdate()
    {
        zMax = zMax.MoveTowards(zMaxMaxDelta, playerProxy.position.z);

        Vector3 targetPos = new(Mathf.Lerp(xMin, xMax, xPercentage), yPos, Mathf.Lerp(zMin, zMax, zPercentage));

        calculatedHeadTarget.position = Vector3.MoveTowards(calculatedHeadTarget.position, targetPos, headMovementMaxDelta);

    }

}
