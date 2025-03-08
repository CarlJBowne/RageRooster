using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSourceExplosion : AttackSourceBase
{

    public Attack attack;
    public MonoBehaviour sourceEntity;

    [Header("Explosion Properties")]
    [SerializeField] private float explosionRadius;
    [SerializeField] private float explosionForce;
    [SerializeField] Transform explosionOrigin;


    Collider currentCollider;

    public override void Contact(GameObject target)
    {

        if (!enabled)
        {
            return;
        }

        currentCollider = target.GetComponent<Collider>();
        if (target.TryGetComponent(out Health health))
        {
            health.Damage(GetAttack());
        }

        /*//Get all colliders within the defined range
        Collider[] collisions = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider collider in collisions)
        {
            currentCollider = collider; //Store the current collider for use in the GetAttack function
            if (target.TryGetComponent(out Health health))
            {
                health.Damage(GetAttack());
                Debug.Log(collider.name);
            }

        }*/
    
    }

    public override Attack GetAttack()
    {
        //When the bomb is triggered
        Attack result = attack;
        Vector3 contact = currentCollider.ClosestPoint(transform.position);
        result.velocity = CalculateKnockback(contact); //Simple function to find the opposite direction and apply knockback based on the closest contact point
        return result;
    }

    public Vector3 CalculateKnockback(Vector3 contactPoint)
    {
        Vector3 dir = contactPoint - transform.position;
        dir = -dir.normalized;
        dir *= explosionForce;
        return dir;
    }


}
