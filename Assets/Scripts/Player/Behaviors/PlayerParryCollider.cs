using RageRooster.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerParryCollider : MonoBehaviour, IAttackSource
{

    public Attack baseAttack;
    public Attack hellUpgradedAttack;
    public new SphereCollider collider;

    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);
    

    public void Contact(GameObject target)
    {
        if (target == Player.GameObject) return;
        if (target.TryGetComponent(out AttackProjectile targetProjectile))
        {
            targetProjectile.Reflect(false);
        }
        else if (target.TryGetComponent(out IDamagable targetDamagable)) 
            targetDamagable.Damage(GetAttack(target));
    }
    public Attack GetAttack() => throw new System.NotImplementedException();
    public Attack GetAttack(GameObject target)
    {
        Attack result = Upgrades.Active.hellcopter ? hellUpgradedAttack : baseAttack;
        result.velocity = (target.transform.position - (transform.position + collider.center)).normalized * result.velocity.x;
        return result;
    }

}
