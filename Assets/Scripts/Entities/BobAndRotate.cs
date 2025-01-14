using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BobAndTurn : MonoBehaviour
{
    public float bobSpeed = 1f;
    public float bobHeight = 0.5f;
    public float turnSpeed = 1f;
    public float turnAngle = 90f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        // Bob up and down
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Turn left and right
        float turnAmount = Mathf.Sin(Time.time * turnSpeed) * turnAngle;
        transform.rotation = startRotation * Quaternion.Euler(0f, turnAmount, 0f);
    }
}