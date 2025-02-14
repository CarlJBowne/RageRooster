using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The base form of Attack Source. A singular possible attack.
/// </summary>
[System.Obsolete]
public class AttackSource_Old : MonoBehaviour, IAttacker_Old
{
    public Attack_Old attack;
    public Vector3 velocity;

    void OnTriggerEnter(Collider other) => Contact(other.gameObject);

    void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);

    public virtual void Contact(GameObject target) => (this as IAttacker_Old).BeginAttack(target, attack, velocity);

}

[System.Obsolete]
public interface IAttacker_Old
{
    public abstract void Contact(GameObject target);

    public void BeginAttack(GameObject target, Attack_Old attack, Vector3 velocity)
    {
        if (!target.TryGetComponent(out Health targetHealth)) return;

        if (targetHealth.Damage(new Attack_Old(attack, this, velocity)))
        {

        }
        else
        {

        }

    }
}