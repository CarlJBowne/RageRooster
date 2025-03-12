using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class ChargeAttackEB : StateBehavior
{
    Rigidbody rb;
    Vector3 direction;
    Transform playerTransform;
    public State vulnerableState;
    float wallCheckDistance = 0.5f;
    public override void OnEnter(State prev, bool isFinal)
    {
        rb = GetComponentFromMachine<Rigidbody>();
        playerTransform = Gameplay.Player.transform;
        direction = (playerTransform.position - rb.transform.position).XZ().normalized;
        rb.transform.rotation = Quaternion.LookRotation(direction);
    }

    public override void OnFixedUpdate()
    {
        RaycastHit hit;
        if (rb.DirectionCast(direction, wallCheckDistance, 1f, out hit))
        {
            TransitionTo(vulnerableState);
        }
    }
}
