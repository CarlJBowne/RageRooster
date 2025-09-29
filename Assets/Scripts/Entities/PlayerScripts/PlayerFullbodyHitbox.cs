using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete]
public class PlayerFullbodyHitbox : PlayerStateBehavior
{
    public bool autoActivate = true;
    public int attackID;
    public Collider hitBox;





    protected override void OnEnter(State prev, bool isFinal)
    {
        if (autoActivate) SetBoxState(true);
    }
    protected override void OnExit(State next)
    {
        SetBoxState(false);
    }

    public void SetBoxState(bool value)
    {
        hitBox.enabled = value;
    }
}
