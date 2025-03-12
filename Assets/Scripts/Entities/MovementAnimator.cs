using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementAnimator : MonoBehaviour
{
    public float influence = 0;
    public Vector3 relativeVelocity;
    public float angularVelocity;
    public float turnToVelocity;

    private Rigidbody rb;
    private Transform target;
    private void Awake() => TryGetComponent(out rb);

    private void FixedUpdate()
    {
        if(influence > 0)
        {
            rb.velocity = Vector3.MoveTowards(rb.velocity, relativeVelocity, (relativeVelocity.magnitude - rb.velocity.magnitude) * influence);

            float tempAngularVelocity = rb.angularVelocity.y;

            tempAngularVelocity = tempAngularVelocity.MoveTowards((angularVelocity - tempAngularVelocity) * influence, angularVelocity);
            rb.angularVelocity = new(0, tempAngularVelocity, 0);

            Vector3 targetDirection = (target.position - transform.position).XZ();
            transform.eulerAngles = Vector3.RotateTowards(transform.forward, targetDirection, (targetDirection - transform.position).magnitude * influence, 0);
        }
    }
}
