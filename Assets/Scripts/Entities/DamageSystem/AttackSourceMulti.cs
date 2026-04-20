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
    [FormerlySerializedAs("additionalTags")] public Attack.Tag_OLD[] additionalTags_Old;
    public Attack.TagSet additionalTags = new();
    public new bool enabled = true;

    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);
    private void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);

    public Attack GetAttack()
    {
        Attack result = attacks[currentAttackID];
        result.velocity = transform.TransformDirection(result.velocity);
        if (additionalTags_Old.Length > 0) result += additionalTags_Old;
        return result;
    }

    public void Contact(GameObject target)
    {
        if(enabled && target.TryGetComponent(out IDamagable targetDamagable)) targetDamagable.Damage(GetAttack());
    }

    public void TransferTags()
    {
        for (int i = 0; i < attacks.Length; i++) attacks[i].TransferTags();
        Attack.TagSet.TransferFromOldTags(additionalTags_Old, additionalTags);
    }
}