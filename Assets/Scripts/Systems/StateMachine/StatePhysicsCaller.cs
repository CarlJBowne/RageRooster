using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(StateMachine))]
public class StatePhysicsCaller : StateBehavior
{
    private StateBehaviorPhysicsCollision[] collisions;
    private StateBehaviorPhysicsTrigger[] triggers;

    protected override void Initialize()
    {
        collisions = M.stateHolder.GetComponentsInChildren<StateBehaviorPhysicsCollision>();
        triggers = M.stateHolder.GetComponentsInChildren<StateBehaviorPhysicsTrigger>();
    }


    private void OnCollisionEnter(Collision collision)
    {
        for (int i = 0; i < collisions.Length; i++)
            if (collisions[i].isActive()) 
                collisions[i].OnCollisionEnter(collision);
    }
    private void OnCollisionExit(Collision collision)
    {
        for (int i = 0; i < collisions.Length; i++)
            if (collisions[i].isActive())
                collisions[i].OnCollisionExit(collision);
    }
    private void OnTriggerEnter(Collider other)
    {
        for (int i = 0; i < triggers.Length; i++)
            if (triggers[i].isActive())
                triggers[i].OnTriggerEnter(other);

    }
    private void OnTriggerExit(Collider other)
    {
        for (int i = 0; i < triggers.Length; i++)
            if (triggers[i].isActive())
                triggers[i].OnTriggerExit(other);
    }
}

public interface StateBehaviorPhysics
{
    public sealed bool isActive() => (this as StateBehavior).state.active;
}
public interface StateBehaviorPhysicsCollision : StateBehaviorPhysics
{
    void OnCollisionEnter(Collision collision);
    void OnCollisionExit(Collision collision);
}
public interface StateBehaviorPhysicsTrigger : StateBehaviorPhysics
{
    void OnTriggerEnter(Collider other);
    void OnTriggerExit(Collider other);
}
public interface StateBehaviorPhysicsCollision2D : StateBehaviorPhysicsCollision
{
    void OnCollisionEnter2D(Collision2D collision);
    void OnCollisionExit2D(Collision2D collision);
}
public interface StateBehaviorPhysicsTrigger2D : StateBehaviorPhysicsCollision
{
    void OnTriggerEnter2D(Collider2D collision);
    void OnTriggerExit2D(Collider2D collision);
} 