using UnityEngine;
using SLS.StateMachineV3;

public abstract class PlayerStateBehavior : StateBehavior
{
    [HideInInspector] public new PlayerStateMachine StateMachine;
    [HideInInspector] public Input input;
    [HideInInspector] public PlayerMovementBody playerMovementBody;
    [HideInInspector] public PlayerController playerController;
    

    protected override void Initialize()
    {
        StateMachine = base.StateMachine as PlayerStateMachine;
        input = StateMachine.input;
        playerMovementBody = StateMachine.body;
        playerController = StateMachine.controller;
    }
        

    #region States

    public State sGrounded => StateMachine.states["Grounded"];
    public State sCharge => StateMachine.states["Charge"];
    public State sAirborne => StateMachine.states["Airborne"];
    public State sFall => StateMachine.states["Fall"];
    public State sGlide => StateMachine.states["Glide"];
    public State sGroundSlam => StateMachine.states["GroundSlam"];
    public State sAirChargeFall => StateMachine.states["AirChargeFall"];

    #endregion

}
