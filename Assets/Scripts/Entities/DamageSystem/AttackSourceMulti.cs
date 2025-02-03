using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AttackSourceMulti : MonoBehaviour
{
    public int currentAttackID;
    public MonoBehaviour sourceEntity;
    public Attack[] attacks;
    public Attack.Tag[] additionalTags;
    public new bool enabled = true;

    public Attack GetAttack()
    {
        Attack result = attacks[currentAttackID];
        result.velocity = transform.TransformPoint(result.velocity);
        if (additionalTags.Length > 0) result += additionalTags;
        return result;
    }

    public virtual void Contact(GameObject target)
    {
        if(enabled && target.TryGetComponent(out Health health)) health.Damage(GetAttack());
    }

}