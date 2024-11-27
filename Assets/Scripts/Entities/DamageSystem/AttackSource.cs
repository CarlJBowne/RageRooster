using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The base form of Attack Source. A singular possible attack.
/// </summary>
public class AttackSource : MonoBehaviour, IAttacker
{
    public Attack attack;
    public Vector3 velocity;

    void OnTriggerEnter(Collider other) => Contact(other.gameObject);

    void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);

    public virtual void Contact(GameObject target) => (this as IAttacker).BeginAttack(target, attack, velocity);

}

public interface IAttacker
{
    public abstract void Contact(GameObject target);

    public void BeginAttack(GameObject target, Attack attack, Vector3 velocity)
    {
        if (!target.TryGetComponent(out Health targetHealth)) return;

        if (targetHealth.Damage(new Attack(attack, this, velocity)))
        {

        }
        else
        {

        }

    }
}