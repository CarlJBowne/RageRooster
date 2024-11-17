using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A type of Attack Source that can disable itself upon hit. Use for Projectiles.
/// </summary>
public class AttackProjectile : AttackSource
{

    public bool disableOnHit;
    public bool disableOnlyWithHealth;


    public override void Contact(GameObject target) => BeginAttack(target, attack, velocity);

    public void BeginAttack(GameObject target, Attack attack, Vector3 velocity)
    {
        if (!target.TryGetComponent(out Health targetHealth)) return;

        if (targetHealth.Damage(new Attack(attack, this, velocity)))
        {
            if(disableOnHit) Disable();
        }
        else
        {
            if (disableOnHit && !disableOnlyWithHealth) Disable();
        }

    }

    public void Disable()
    {
        if (PoolableObject.Is(gameObject, out PoolableObject P)) P.Disable();
        else Destroy(gameObject);
    }
}