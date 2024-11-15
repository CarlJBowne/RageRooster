using SLS.StateMachineV2;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ShooterEB : StateBehavior
{
    public ObjectPool bulletPool;
    public float rate;
    public float chanceOfFiringWhenBlocked;

    Timer fireTimer;
    TrackerEB tracker;

    public override void OnAwake() 
    {
        bulletPool.Initialize();
        //fireTimer = new(rate, Fire);
        fireTimer = Timer.New(rate, Fire);
        tracker = state.parent.GetComponent<TrackerEB>();
    }

    public override void OnUpdate()
    {
        bulletPool.spawnPoint.rotation = Quaternion.LookRotation(tracker.Direction, Vector3.up);
        fireTimer.Increment(Time.deltaTime);
        bulletPool.Update();
    }

    public void Fire()
    {
        if (tracker.LineOfSight(true) || chanceOfFiringWhenBlocked.RandomChance()) 
            bulletPool.Pump();
    }

}