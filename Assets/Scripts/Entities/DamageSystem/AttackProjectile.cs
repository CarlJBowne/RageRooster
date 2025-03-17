using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackProjectile : AttackSourceSingle
{
    public bool disableOnHit;
    public bool disableOnlyWithHealth;

    public Transform sourcePosition;
    private Rigidbody rb;
    private ConstantMovement cm;

    private void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out cm);
    }

    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);
    private void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);


    public override void Contact(GameObject target)
    {
        if (!enabled) return;

        if(target.TryGetComponent(out IDamagable targetDamagable))
        {
            targetDamagable.Damage(GetAttack());
            if (disableOnHit) Disable();
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

    public void Reflect(bool backAt = false)
    {
        if (sourcePosition == null) backAt = false;

        if (rb)
        {
            rb.velocity = backAt 
                ? (sourcePosition.position - rb.position).normalized * rb.velocity.magnitude 
                : -rb.velocity;
        }
        else if (cm)
        {
            cm.ResetDownwardVelocity();
            Vector3 literalDirection = transform.TransformDirection(cm.direction);
            transform.rotation = Quaternion.FromToRotation(literalDirection, backAt
                ? (sourcePosition.position - transform.position).normalized
                : -literalDirection);
        }
    }
}
