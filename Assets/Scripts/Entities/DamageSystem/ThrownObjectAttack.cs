using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrownObjectAttack : AttackSourceSingle
{

    public UltEvents.UltEvent onContactEvent;
    public System.Action onContactAction;

    public override void Contact(GameObject target)
    {
        base.Contact(target);
        enabled = false;
        onContactAction?.Invoke();
        onContactAction = null;
        onContactEvent?.Invoke();
    }

    private void Reset() => enabled = false;

    private void Awake() => enabled = false;
}
