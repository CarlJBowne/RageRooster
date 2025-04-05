using UnityEngine;
using SLS.StateMachineV3;

public abstract class PlayerStateBehavior : StateBehavior
{
    [HideInInspector] public new PlayerStateMachine Machine;
    [HideInInspector] public PlayerMovementBody playerMovementBody;
    [HideInInspector] public PlayerController playerController;
    

    protected override void Initialize()
    {
        Machine = base.Machine as PlayerStateMachine;
        playerMovementBody = Machine.body;
        playerController = Machine.controller;
    }
        

    #region States

    public State sGrounded => Machine.states["Grounded"];
    public State sCharge => Machine.states["Charge"];
    public State sAirborne => Machine.states["Airborne"];
    public State sFall => Machine.states["Fall"];
    public State sGlide => Machine.states["Glide"];
    public State sGroundSlam => Machine.states["GroundSlam"];
    public State sAirChargeFall => Machine.states["AirChargeFall"];

    #endregion

}
