using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RageRooster.Services;
public class MovementAnimator : MonoBehaviour
{
    [Header("Only touch if animating.")]
    public float influence = 0;
    public Vector3 relativeVelocity;
    public float angularVelocity;
    public float turnToVelocity;
    public float snapToGroundDistance = 4f;
    public LayerMask groundLayerMask;

    private Rigidbody rb;
    private Transform target;
    [SerializeField, DisableInPlayMode, HideInEditMode] private Vector3 velocityDisplay;
    private void Awake()
    {
        TryGetComponent(out rb);
        target = Player.Transform;
        influence = 0;
        relativeVelocity = Vector3.zero;
        angularVelocity = 0;
        turnToVelocity = 0;
    }

    private void FixedUpdate()
    {
        if (influence == 1)
        {
            rb.linearVelocity = transform.TransformDirection(relativeVelocity);

            if (!Mathf.Approximately(angularVelocity, 0)) transform.eulerAngles = transform.eulerAngles + angularVelocity * transform.up;
            else if (turnToVelocity > 0)
            {
                Vector3 targetDirection = target.position - transform.position;
                targetDirection.y = 0;
                transform.eulerAngles = Vector3.RotateTowards(transform.forward, targetDirection, turnToVelocity * 2 * Mathf.PI, 0).DirToRot();
            }
        }
        else if (influence > 0)
        {
            Vector3 trueRelativeVelocity = transform.TransformDirection(relativeVelocity);
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, trueRelativeVelocity, influence * (trueRelativeVelocity - rb.linearVelocity).magnitude);

            if (!Mathf.Approximately(angularVelocity, 0))
            {
                transform.eulerAngles = transform.eulerAngles + influence * angularVelocity * transform.up;
            }
            else if (turnToVelocity > 0)
            {
                Vector3 targetDirection = target.position - transform.position;
                targetDirection.y = 0;
                transform.eulerAngles = Vector3.RotateTowards(transform.forward, targetDirection, influence * turnToVelocity * 2 * Mathf.PI, 0).DirToRot();
            }
        }
        if (influence > 0)
        {
            if (snapToGroundDistance > 0 && Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, snapToGroundDistance, groundLayerMask))
            {
                rb.MovePosition(hitInfo.point + Vector3.up * 0.001f);
                rb.linearVelocity = rb.linearVelocity.XZ();
            }
        }
        velocityDisplay = rb.linearVelocity;
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
}