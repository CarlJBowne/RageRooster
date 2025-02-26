using SLS.StateMachineV3;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StopAndShootEB : StateBehavior
{
    public ObjectPool bulletPool;
    public float rate;
    public float chanceOfFiringWhenBlocked;
    private NavMeshAgent agent;

    Timer_Old fireTimer;
    TrackerEB tracker;

    public override void OnAwake() 
    {
        agent = GetComponentFromMachine<NavMeshAgent>();
        bulletPool.Initialize();
        //fireTimer = new(rate, Fire);
        fireTimer = new(rate, Fire);
        tracker = state.parent.GetComponent<TrackerEB>();
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        agent.enabled = false;
    }

    public override void OnUpdate()
    {
        bulletPool.spawnPoint.rotation = Quaternion.LookRotation(tracker.Direction, Vector3.up);
        fireTimer.Increment(Time.deltaTime, Fire);
        bulletPool.Update(); 
    }

    public void Fire()
    {
        if (tracker.LineOfSight(true) || chanceOfFiringWhenBlocked.RandomChance()) 
            bulletPool.Pump();
    }

}
