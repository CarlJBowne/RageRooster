using System;
using System.Collections.Generic;
using UnityEngine;

public class ConstantMovement : MonoBehaviour
{
    public float speed;
    public Vector3 direction;

    private void FixedUpdate() => transform.position = transform.position + transform.TransformDirection(direction * speed * Time.fixedDeltaTime);

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
