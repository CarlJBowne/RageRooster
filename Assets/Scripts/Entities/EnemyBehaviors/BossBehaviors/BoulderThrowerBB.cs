using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoulderThrowerBB : StateBehavior
{
    public float inaccuracy;
    public float throwTime;
    public float minVelocity;

    public ObjectPool projectiles;
    public ObjectPool warnings;
    public Transform muzzle;

    private Transform target;

    public override void OnAwake()
    {
        target = Gameplay.Player.transform;
    }

    public void Launch()
    {
        if (projectiles.prefabObject == null) return;

        Vector3 trueTarget = target.position + inaccuracy * Random.insideUnitCircle.ToXZ();
        Vector3 targetDistance = trueTarget - muzzle.position;
        Vector2 targetDistanceXY = new(targetDistance.XZ().magnitude, targetDistance.y);

        warnings.Pump().SetPosition(trueTarget);

        muzzle.eulerAngles = (trueTarget - muzzle.position).XZ().DirToRot();
        PoolableObject boulder = projectiles.Pump();

        PhysicsPro.ThrowAt.WithTimeAndMinVelocity(targetDistanceXY, throwTime, -Physics.gravity.y, minVelocity, out float initialVelocity, out float angle);
        muzzle.eulerAngles.Rotate(angle, muzzle.right);
        boulder.rb.velocity = initialVelocity * muzzle.forward;

    }


}
