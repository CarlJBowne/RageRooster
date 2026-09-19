using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSourceExplosion : AttackSourceSingle
{

    //public Attack attack;
    //public MonoBehaviour sourceEntity;

    //[Header("Explosion Properties")]
    //[SerializeField] private float explosionRadius;
    [SerializeField] private float explosionForce;
    //[SerializeField] Transform explosionOrigin;

    //Collider currentCollider;

    public override void Contact(GameObject target)
    {

        if (!enabled)
        {
            return;
        }

        //currentCollider = target.GetComponent<Collider>();
        if (target.TryGetComponent(out IDamagable targetDamagable)) targetDamagable.Damage(GetAttack(target.transform)); 

    }

    public Attack GetAttack(Transform target)
    {
        Attack result = attack;
        //Vector3 contact = currentCollider.ClosestPoint(transform.position);
        result.velocity = (target.position - transform.position).normalized * explosionForce; //Simple function to find the opposite direction and apply knockback based on the closest contact point
        return result;

    }

    public override Attack GetAttack() => throw new NotImplementedException();

    //public Vector3 CalculateKnockback(Vector3 contactPoint)
    //{
    //    Vector3 dir = contactPoint - transform.position;
    //    dir = -dir.normalized;
    //    dir *= explosionForce;
    //    return dir;
    //}


}
