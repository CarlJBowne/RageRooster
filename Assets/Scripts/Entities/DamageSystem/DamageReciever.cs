using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DamageReciever : MonoBehaviour, IDamagable
{
    public Health targetHealth;
    public string attachTag;

    private void Awake()
    {
        if (targetHealth == null) targetHealth = GetComponentInParent<Health>();
        if (targetHealth == null) Destroy(this); 
    }

    public bool Damage(Attack attack)
    {
        if (!enabled) return false;

        attack.tags = attack.tags.ToArray().Append(attachTag).ToArray();

        return targetHealth.Damage(attack);
    }
}
