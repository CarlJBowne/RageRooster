using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoulderThrowerBB : MonoBehaviour
{
    public float inaccuracy;
    public float throwTime;
    public float minVelocity;

    public ObjectPool projectiles;
    public ObjectPool warnings;
    public Transform fakeMuzzle;
    public Transform trueMuzzle;

    private Transform target;

    public void Awake()
    {
        target = Gameplay.Player.transform;
    }

    private void Update()
    {
        warnings.Update();
        projectiles.Update();
    }

    public void Launch()
    {
        if (projectiles.prefabObject == null) return;

        Vector3 trueTarget = target.position + (inaccuracy * Random.insideUnitCircle.ToXZ());
        trueMuzzle.position = fakeMuzzle.position;
        Vector3 targetDistance = trueTarget - trueMuzzle.position;
        trueMuzzle.eulerAngles = targetDistance.XZ().DirToRot();
        Vector2 targetDistanceXY = new(targetDistance.XZ().magnitude, targetDistance.y);

        warnings.Pump().SetPosition(trueTarget);

        PhysicsPro.ThrowAt.WithTimeAndMinVelocity(targetDistanceXY, throwTime, -Physics.gravity.y, minVelocity, out float initialVelocity, out float angle);

        trueMuzzle.eulerAngles = trueMuzzle.eulerAngles - (Vector3.right * angle);
        PoolableObject boulder = projectiles.Pump();
        boulder.SetPosition(trueMuzzle.position);
        boulder.rb.velocity = initialVelocity * trueMuzzle.forward; 

    }


}
