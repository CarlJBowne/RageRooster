using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.ObjectUtilities;
using static RageRooster.Services;

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
        target = Player.Transform;
    }

    private void Update()
    {
        warnings.Update(Time.deltaTime);
        projectiles.Update(Time.deltaTime);
    }

    public void Launch()
    {
        if (projectiles.prefab == null) return;

        Vector3 trueTarget = target.position + (inaccuracy * Random.insideUnitCircle.ToXZ());
        trueMuzzle.position = fakeMuzzle.position;
        Vector3 targetDistance = trueTarget - trueMuzzle.position;
        trueMuzzle.eulerAngles = targetDistance.XZ().DirToRot();
        Vector2 targetDistanceXY = new(targetDistance.XZ().magnitude, targetDistance.y);

        warnings.Pump(trueTarget);

        SLS.Physics3D.Helpers.ThrowAt.WithTimeAndMinVelocity(targetDistanceXY, throwTime, -Physics.gravity.y, minVelocity, out float initialVelocity, out float angle);

        trueMuzzle.eulerAngles -= Vector3.right * angle;
        Spawnable boulder = projectiles.Pump(trueMuzzle);
        boulder.GetComponent<Rigidbody>().linearVelocity = initialVelocity * trueMuzzle.forward;

    }


}
