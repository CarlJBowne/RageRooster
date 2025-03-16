using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class ChargeAttackEB : StateBehavior
{
    Rigidbody rb;
    //Vector3 direction;
    //Transform playerTransform;

    public State vulnerableState;
    public LayerMask layerMask;
    public float checkDistance;
    //float wallCheckDistance = 0.5f;
    public override void OnEnter(State prev, bool isFinal)
    {
        rb = GetComponentFromMachine<Rigidbody>();
        //playerTransform = Gameplay.Player.transform;
        //direction = (playerTransform.position - rb.transform.position).XZ().normalized;
        //rb.transform.rotation = Quaternion.LookRotation(direction);
    }

    public override void OnFixedUpdate()
    {
        //if (rb.DirectionCast(transform.forward, wallCheckDistance, 1f, out RaycastHit hit))
        //{
        //    TransitionTo(vulnerableState);
        //}
        if (Physics.Raycast(transform.position + rb.centerOfMass, transform.forward, out RaycastHit hitInfo, (rb.velocity.magnitude * .02f)+ checkDistance, layerMask, QueryTriggerInteraction.Ignore)) 
            vulnerableState.TransitionTo();
    }
}
