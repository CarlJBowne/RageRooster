using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementAnimator : MonoBehaviour
{
    [Header("Only touch if animating.")]
    public float influence = 0;
    public Vector3 relativeVelocity;
    public float angularVelocity;
    public float turnToVelocity;
    public bool snapToGround;
    public LayerMask groundLayerMask;

    private Rigidbody rb;
    private Transform target;
    private void Awake()
    {
        TryGetComponent(out rb);
        target = Gameplay.Player.transform;
        influence = 0;
        relativeVelocity = Vector3.zero;
        angularVelocity = 0;
        turnToVelocity = 0;
    }

    private void FixedUpdate()
    {
        if(influence > 0)
        {
            Vector3 trueRelativeVelocity = transform.TransformDirection(relativeVelocity);
            rb.velocity = Vector3.MoveTowards(rb.velocity, trueRelativeVelocity, influence * (trueRelativeVelocity - rb.velocity).magnitude);

            if(!Mathf.Approximately(angularVelocity, 0))
            {
                transform.eulerAngles = transform.eulerAngles + influence * angularVelocity * transform.up;
            }
            else if(turnToVelocity > 0)
            {
                Vector3 targetDirection = target.position - transform.position;
                targetDirection.y = 0;
                transform.eulerAngles = Vector3.RotateTowards(transform.forward, targetDirection, influence * turnToVelocity * 2 * Mathf.PI, 0).DirToRot();
            }

            if (snapToGround && Physics.Raycast(rb.centerOfMass, Vector3.down, out RaycastHit hitInfo, 100f, groundLayerMask))
                transform.position = hitInfo.point;
        }
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
}
