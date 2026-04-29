using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Serialization;

public class AttackSourceMulti : MonoBehaviour, IAttackSource
{
    public int currentAttackID;
    public MonoBehaviour sourceEntity;
    public Attack[] attacks;
    public Attack.TagSet additionalTags = new();
    public new bool enabled = true;

    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);
    private void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);

    public Attack GetAttack()
    {
        Attack result = attacks[currentAttackID];
        result.velocity = transform.TransformDirection(result.velocity);
        result.tags += additionalTags;
        return result;
    }

    public void Contact(GameObject target)
    {
        if(enabled && target.TryGetComponent(out IDamagable targetDamagable)) targetDamagable.Damage(GetAttack());
    }

}