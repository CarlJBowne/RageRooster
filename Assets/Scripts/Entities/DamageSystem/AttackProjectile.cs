using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackProjectile : AttackSourceSingle
{
    public bool disableOnHit;
    public bool disableOnlyWithHealth;
    public bool disableOnlyIfSuccessfulHit;
    public float contactableTimer = 0.333333f;


    public Transform sourcePosition;
    private Rigidbody rb;
    private ConstantMovement cm;
    private float timeWhenContactable;

    private void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out cm);
    }
    private void OnEnable()
    {
        if(contactableTimer > 0) timeWhenContactable = Time.time + contactableTimer;
    }

    private void OnTriggerEnter(Collider other)
    { 
        Contact(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Contact(collision.gameObject);
    }


    public override void Contact(GameObject target)
    {
        if (!enabled || (Time.time < timeWhenContactable)) return;

        if(target.TryGetComponent(out IDamagable targetDamagable))
        {
            bool success = targetDamagable.Damage(GetAttack());
            if (disableOnHit && (!disableOnlyIfSuccessfulHit || success)) Disable();
        }
        else
        { 
            if (disableOnHit && !disableOnlyWithHealth) Disable();
        }
    }

    public void Disable() => PoolableObject.DisableOrDestroy(gameObject);

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
