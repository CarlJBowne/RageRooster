using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using AYellowpaper;

public class DamageReciever : MonoBehaviour, IDamagable
{
    public InterfaceReference<IDamagable, Component> target;
    public Attack.TagSet appendedTags;

    private void Awake()
    {
        if (target.Value == null) target = new(GetComponentInParent<IDamagable>());
        if (target.Value == null) Destroy(this);
    }

    public bool Damage(Attack attack)
    {
        if (!enabled) return false;

        attack.tags += appendedTags;

        return target.Value.Damage(attack);
    }
}
