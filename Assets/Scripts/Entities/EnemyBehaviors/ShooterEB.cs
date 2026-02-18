using SLS.StateMachineH;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ShooterEB : StateBehavior
{
    public ObjectPool_OBSOLETE bulletPool;
    public float rate;
    public float chanceOfFiringWhenBlocked;

    Timer.Loop fireTimer;
    TrackerEB tracker;

    protected override void OnAwake() 
    {
        bulletPool.Initialize();
        //fireTimer = new(rate, Fire);
        fireTimer = new(rate);
        tracker = State.Parent.GetComponent<TrackerEB>();
    }

    protected override void OnUpdate()
    {
        bulletPool.spawnPoint.rotation = Quaternion.LookRotation(tracker.Direction, Vector3.up);
        fireTimer.Tick(Fire);
        bulletPool.Update(); 
    }

    public void Fire()
    {
        if (tracker.LineOfSight(true) || chanceOfFiringWhenBlocked.RandomChance()) 
            bulletPool.Pump();
    }

}