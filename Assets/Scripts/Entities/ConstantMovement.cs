using System;
using System.Collections.Generic;
using UnityEngine;

public class ConstantMovement : MonoBehaviour
{
    public float speed;
    public Vector3 direction;
    public float gravity;

    private Rigidbody rb;
    private float downwardsVelocity;

    private void Awake() => TryGetComponent(out rb);

    private void FixedUpdate()
    {
        if (!rb)
        {
            Vector3 finalPosition = transform.position + transform.TransformDirection(direction * speed * Time.fixedDeltaTime);
            if(gravity > 0)
            {
                finalPosition += Vector3.down * downwardsVelocity;
                downwardsVelocity -= gravity * Time.fixedDeltaTime;
            }
            transform.position = finalPosition;
        }
        else
        {
            float preEffect = rb.velocity.y;
            Vector3 finalVelocity = transform.TransformDirection(direction * speed * Time.fixedDeltaTime);
            finalVelocity += Vector3.down * (preEffect - finalVelocity.y);
            rb.velocity = finalVelocity;
        }
    }

    private void OnEnable()
    {
        if (rb) rb.velocity = Vector3.zero;
        else downwardsVelocity = 0;
    }

    public void Set(Vector3 value)
    {
        speed = value.magnitude;
        direction = value.normalized;
    }
    public void Set(float speed, Vector3 direction)
    {
        this.speed = speed;
        this.direction = direction;
    }
}
