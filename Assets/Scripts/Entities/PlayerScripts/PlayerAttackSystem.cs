using SLS.StateMachineV2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackSystem : AttackSource
{

    #region Config
    #endregion
    #region States

    public State airborneState;
    public State groundSlamState;

    #endregion
    #region Data
    #endregion


    public void AttackButtonPress()
    {
        if (airborneState.active && !groundSlamState.active) BeginGroundSlam();
    }

    public void BeginGroundSlam()
    {

    }




}
