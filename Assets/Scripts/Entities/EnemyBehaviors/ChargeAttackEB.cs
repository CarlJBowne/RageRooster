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

    public new SphereCollider collider;
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

        if (Physics.SphereCast(
            transform.position + collider.center + Vector3.up * collider.radius/2,
            collider.radius/2,
            transform.forward,
            out RaycastHit hitInfo,
            (rb.velocity.magnitude * .02f) + checkDistance + collider.radius,
            layerMask,
            QueryTriggerInteraction.Ignore)
            && hitInfo.normal.y < .65f)
            vulnerableState.TransitionTo();
    }
}
