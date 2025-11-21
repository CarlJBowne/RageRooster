using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrownObjectAttack : AttackSourceSingle
{

    public System.Action onContactAction;

    public override void Contact(GameObject target)
    {
        base.Contact(target);
        enabled = false;
        onContactAction?.Invoke();
        onContactAction = null;
    }

    private void Reset() => enabled = false;

    private void Awake() => enabled = false;
}
