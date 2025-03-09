using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AttackSourceSingle : MonoBehaviour, IAttackSource
{
    public Attack attack;
    public MonoBehaviour sourceEntity;
    public new bool enabled = true;

    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);
    private void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);

    public virtual Attack GetAttack()
    {
        Attack result = attack;
        result.velocity = transform.TransformDirection(result.velocity);
        return result;
    }

    public virtual void Contact(GameObject target)
    {
        if (enabled && target.TryGetComponent(out IDamagable targetDamagable)) targetDamagable.Damage(GetAttack());
    }

}