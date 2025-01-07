using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete]
public class PlayerFullbodyHitbox : PlayerStateBehavior
{
    public bool autoActivate = true;
    public string attackName;
    public AttackHitBox hitBox;





    public override void OnEnter(State prev)
    {
        if (autoActivate) SetBoxState(true);
    }
    public override void OnExit(State next)
    {
        hitBox.Disable();
    }

    public void SetBoxState(bool value)
    {
        hitBox.SetActive(value);
        if(value) hitBox.currentAttackID = attackName;
    }
}
