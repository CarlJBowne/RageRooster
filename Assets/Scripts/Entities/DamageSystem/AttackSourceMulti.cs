using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AttackSourceMulti : AttackSourceBase
{
    public int currentAttackID;
    public MonoBehaviour sourceEntity;
    public Attack[] attacks;
    public Attack.Tag[] additionalTags;
    public new bool enabled = true;

    public override Attack GetAttack()
    {
        Attack result = attacks[currentAttackID];
        result.velocity = transform.TransformDirection(result.velocity);
        if (additionalTags.Length > 0) result += additionalTags;
        return result;
    }

    public override void Contact(GameObject target)
    {
        if(enabled && target.TryGetComponent(out Health health)) health.Damage(GetAttack());
    }

}