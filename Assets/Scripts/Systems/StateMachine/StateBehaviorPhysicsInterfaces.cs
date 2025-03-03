
using SLS.StateMachineV3;
using UnityEngine;

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
public interface StateBehaviorPhysicsCollision2D : StateBehaviorPhysics
{
    void OnCollisionEnter2D(Collision2D collision);
    void OnCollisionExit2D(Collision2D collision);
}
public interface StateBehaviorPhysicsTrigger2D : StateBehaviorPhysics
{
    void OnTriggerEnter2D(Collider2D collision);
    void OnTriggerExit2D(Collider2D collision);
}