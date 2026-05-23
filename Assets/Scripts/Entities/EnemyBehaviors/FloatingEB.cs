using SLS.StateMachineH;
using System;
using UnityEngine;
using UnityEngine.AI;

public class FloatingEB : StateBehavior
{
    public float sineSize;
    public float sineSpeed;

    protected override void OnFixedUpdate()
    {
        float offset = Mathf.Sin(Time.time * sineSpeed) * sineSize;
        float revOffset = Mathf.Sin((Time.time-Time.fixedDeltaTime) * sineSpeed) * sineSize;

        Vector3 target = transform.position - Vector3.up * revOffset + Vector3.up * offset;
        transform.position = Vector3.MoveTowards(transform.position, target, sineSpeed);

        
    }
}