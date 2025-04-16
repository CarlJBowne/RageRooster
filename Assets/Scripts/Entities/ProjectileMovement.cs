using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileMovement : MonoBehaviour
{
    public Vector3 initialVelocity;
    public float gravity;

    Rigidbody rb;

    private void Awake()
    {
        TryGetComponent(out rb);
        rb.useGravity = false;
    }
    private void OnEnable() => Send();
    public void Send()
    {
        rb.velocity = transform.TransformDirection(initialVelocity);
    }
    private void FixedUpdate()
    {
        if (gravity > 0) rb.velocity += Vector3.down * gravity * Time.fixedDeltaTime;
    }
}
