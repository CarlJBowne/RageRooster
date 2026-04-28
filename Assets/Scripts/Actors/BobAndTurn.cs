using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BobAndTurn : MonoBehaviour
{
    //public float bobSpeed = 1f;
    public float bobHeight = 0.5f;
    //public float turnSpeed = 1f;
    //public float turnAngle = 90f;

    private Vector3 startPosition;

    private void OnEnable()
    {
        startPosition = transform.position;
        Gameplay.bobAndTurnList.Add(this);
    }

    private void OnDisable()
    {
        Gameplay.bobAndTurnList.Remove(this);
    }

    public void DoUpdate(float bob, float rotate)
    {
        transform.position = new (transform.position.x, startPosition.y + bob * bobHeight, transform.position.z);

        transform.eulerAngles = new(0, rotate, 0);
    }
}