using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructibleWall : Health
{
    public Attack.TagSet requiredTags;
    public bool basicDelete = true;

    protected override bool OverrideDamageable(Attack attack) => attack.tags.ContainsAllOf(requiredTags);
    protected override void OnDeplete(Attack attack)
    { if (basicDelete) gameObject.SetActive(false); }

}
