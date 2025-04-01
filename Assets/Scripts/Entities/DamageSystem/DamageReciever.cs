using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DamageReciever : MonoBehaviour, IDamagable
{
    public IRef<IDamagable> target;
    public string attachTag;

    private void Awake()
    {
        if (!target) target = GetComponentInParent<IDamagable>() as IRef<IDamagable>;
        if (!target) Destroy(this); 
    }

    public bool Damage(Attack attack)
    {
        if (!enabled) return false;

        attack.tags = attack.tags.ToArray().Append(attachTag).ToArray();

        return target.I.Damage(attack);
    }
}
