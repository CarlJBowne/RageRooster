using SLS.StateMachineV3;
using UnityEngine;
using System.Linq;
using EditorAttributes;
using UnityEngine.InputSystem;
using Cinemachine;
using System.Collections.Generic;
using CTX = UnityEngine.InputSystem.InputAction.CallbackContext;

public class PlayerController : PlayerStateBehavior
{
	#region Config

	public float jumpBuffer = 0.3f;

    public PlayerAirborneMovement airChargeState;
    public PlayerAirborneMovement airChargeFallState;
	public PlayerWallJump wallJumpState;
    public PlayerRanged ranged;
    public PlayerAiming aimingState;
    public State groundedSpin;
    public State airSpin;
    public PlayerHellcopterMovement airUpwardTornado;
    public State ventGlideState; 
    public Upgrade groundSlamUpgrade;
	public Upgrade wallJumpUpgrade;
    public Upgrade ragingChargeUpgrade;
    public Upgrade hellcopterUpgrade;

    public System.Action onAnimatorMove;

	#endregion
	#region Data

	[HideProperty] public float jumpInput;
	[HideProperty] public Vector3 camAdjustedMovement;
	[HideProperty] public PlayerRanged grabber;
    [HideProperty] public PlayerInteracter interacter;

    #endregion
    #region Getters

    #endregion

    public override void OnAwake()
	{
		if(!grabber) grabber = GetComponentFromMachine<PlayerRanged>();
        if(!interacter) interacter = GetComponentFromMachine<PlayerInteracter>();

		input.jump.performed            += BeginActionEvent;
        input.attackTap.performed       += BeginActionEvent;
        input.attackHold.performed      += BeginActionEvent;
        input.grabTap.performed         += BeginActionEvent;
        input.parry.performed           += BeginActionEvent;
        input.chargeTap.performed       += BeginActionEvent;
        input.chargeHold.performed      += BeginActionEvent;
        input.interact.performed        += BeginActionEvent;

        input.jump.canceled             += JumpRelease;
        input.shootMode.performed       += ShootModeActivate;
        input.shootMode.canceled        += ShootModeDeactivate;
    }


	private void OnDestroy()
	{
        input.jump.performed            -= BeginActionEvent;
        input.attackTap.performed       -= BeginActionEvent;
        input.attackHold.performed      -= BeginActionEvent;
        input.grabTap.performed         -= BeginActionEvent;
        input.parry.performed           -= BeginActionEvent;
        input.chargeTap.performed       -= BeginActionEvent;
        input.chargeHold.performed      -= BeginActionEvent;
        input.interact.performed        -= BeginActionEvent;

        input.jump.canceled             -= JumpRelease;
        input.shootMode.performed       -= ShootModeActivate;
        input.shootMode.canceled        -= ShootModeDeactivate;

    }

    public override void OnUpdate()
	{

		if (jumpInput > 0) jumpInput -= Time.deltaTime;
		camAdjustedMovement = input.movement.ToXZ().Rotate(Machine.cameraTransform.eulerAngles.y, Vector3.up);

		//if (Machine.signalReady && input.jump.IsPressed() && sFall && !grabber.currentGrabbed) 
        //    sGlide.TransitionTo();
        //else if(Machine.signalReady && !input.jump.IsPressed() && sGlide) 
        //    sFall.TransitionTo();

        if (Machine.freeLookCamera != null)
        {
            Machine.freeLookCamera.Follow = transform;
            Machine.freeLookCamera.LookAt = transform;
        }

        if (input.shootMode.IsPressed() && Machine.signalReady && sGrounded 
            && !ranged.aimingState && (ranged.hasEggsToShoot || grabber.currentGrabbed != null)) 
            ranged.EnterAiming();
    }

    public bool CheckJumpBuffer()
    {
        bool result = jumpInput > 0;
        jumpInput = 0;
        return result;
    }
    public void BeginJumpInputBuffer() => jumpInput = jumpBuffer + Time.fixedDeltaTime;

    private void OnAnimatorMove() => onAnimatorMove?.Invoke();


    private void BeginActionEvent(InputAction.CallbackContext callbackContext) => Machine.SendSignal(callbackContext.action.name);
    public void BeginActionEvent(string name) => Machine.SendSignal(name);

    public void ReadyNextAction() => Machine.ReadySignal();
    public void FinishAction() => Machine.FinishSignal();



    public void ParryActionAirborne()
    {
        if(hellcopterUpgrade)
        {
            airSpin.TransitionTo();
            if (playerMovementBody.isOverVent) Machine.SendSignal("EnterVent", addToQueue: false, overrideReady: true);
        }
    }

    public void MidJumpJumpAction()
    {
        if (!wallJumpState.WallJump(transform.forward))
        {
            (!playerMovementBody.isOverVent ? sGlide : ventGlideState).TransitionTo();
        }
    }

    //Other events.
    private void JumpRelease(CTX ctx) => Machine.SendSignal("JumpRelease", false, true);
    private void ShootModeActivate(CTX ctx) => Machine.SendSignal("ShootMode", true, true);
    private void ShootModeDeactivate(CTX ctx) => Machine.SendSignal("ShootModeExit", true, true);




}