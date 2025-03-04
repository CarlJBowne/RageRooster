using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AttackSourceSingle : AttackSourceBase
{
    public Attack attack;
    public MonoBehaviour sourceEntity;
    public new bool enabled = true;

    public override Attack GetAttack()
    {
        Attack result = attack;
        result.velocity = transform.TransformDirection(result.velocity);
        return result;
    }

    public override void Contact(GameObject target)
    {
        if (enabled && target.TryGetComponent(out Health health)) health.Damage(GetAttack());
    }

}