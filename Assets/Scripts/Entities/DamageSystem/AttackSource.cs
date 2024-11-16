using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The base form of Attack Source. A singular possible attack.
/// </summary>
public class AttackSource : MonoBehaviour
{
    public Attack attack;
    public Vector3 velocity;

    void OnTriggerEnter(Collider other) => Contact(other.gameObject);

    void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);

    public virtual void Contact(GameObject target) => BeginAttack(target, attack);

    public virtual void BeginAttack(GameObject target, Attack attack)
    {
        if (!target.TryGetComponent(out Health targetHealth)) return;

        if (targetHealth.Damage(new Attack(attack, this, velocity)))
        {
            //Succesful Strike Additional Logic here.
        }
        else
        {
            //Unsuccesful Strike Additional Logic here.
        }

    }

}
