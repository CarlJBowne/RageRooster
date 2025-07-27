using SLS.StateMachineV3;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StopAndShootEB : StateBehavior
{
    public ObjectPool bulletPool = new();
    public Timer.Loop fireRate;
    public float chanceOfFiringWhenBlocked; 
    private NavMeshAgent agent;

    TrackerEB tracker;

    public override void OnAwake() 
    {
        agent = GetComponentFromMachine<NavMeshAgent>();
        bulletPool.Initialize();
        tracker = state.parent.GetComponent<TrackerEB>();
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        agent.enabled = false;
    }

    public override void OnUpdate()
    {
        bulletPool.spawnPoint.rotation = Quaternion.LookRotation(tracker.Direction, Vector3.up);
        fireRate.Tick(Fire);
        bulletPool.Update(); 
    }

    public void Fire()
    {
        if (tracker.LineOfSight(true) || chanceOfFiringWhenBlocked.RandomChance()) 
            bulletPool.Pump();
    }

}
