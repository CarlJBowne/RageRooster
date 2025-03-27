using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructibleWall : Health
{
    public string requiredTag;
    public bool basicDelete = true;

    protected override bool OverrideDamageable(Attack attack) => attack.HasTag(requiredTag);
    protected override void OnDeplete(Attack attack)
    {if (basicDelete) gameObject.SetActive(false);}

}
