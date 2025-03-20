using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Boss2Health : Health
{
    protected override bool OverrideDamageable(Attack attack)
    {
        if (attack.HasTag("OnWeakSpot"))
        {
            return true;
        }
        return false;
    }
}
