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

	public PlayerWallJump wallJumpState;
    public PlayerAirborneMovement airChargeState;
    public PlayerAirborneMovement airChargeFallState;
    public PlayerRanged ranged;
    public PlayerAiming aimingState;
	public Upgrade groundSlamUpgrade;
	public Upgrade wallJumpUpgrade;
    public Upgrade ragingChargeUpgrade;

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
        input.grabHold.performed        += BeginActionEvent;
        input.parry.performed           += BeginActionEvent;
        input.chargeTap.performed       += BeginActionEvent;
        input.chargeHold.performed      += BeginActionEvent;
        input.shoot.performed           += BeginActionEvent;

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
        input.grabHold.performed        -= BeginActionEvent;
        input.parry.performed           -= BeginActionEvent;
        input.chargeTap.performed       -= BeginActionEvent;
        input.chargeHold.performed      -= BeginActionEvent;
        input.shoot.performed           -= BeginActionEvent;

        input.jump.canceled             -= JumpRelease;
        input.shootMode.performed       -= ShootModeActivate;
        input.shootMode.canceled        -= ShootModeDeactivate;

    }

    public override void OnUpdate()
	{

		if (jumpInput > 0) jumpInput -= Time.deltaTime;
		camAdjustedMovement = input.movement.ToXZ().Rotate(StateMachine.cameraTransform.eulerAngles.y, Vector3.up);

		if (StateMachine.signalReady && input.jump.IsPressed() && sFall && !grabber.currentGrabbed) 
            sGlide.TransitionTo();
        else if(StateMachine.signalReady && !input.jump.IsPressed() && sGlide) 
            sFall.TransitionTo();

        if (StateMachine.freeLookCamera != null)
        {
            StateMachine.freeLookCamera.Follow = transform;
            StateMachine.freeLookCamera.LookAt = transform;
        }

        if (input.shootMode.IsPressed() && StateMachine.signalReady && sGrounded 
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


    private void BeginActionEvent(InputAction.CallbackContext callbackContext) => StateMachine.SendSignal(callbackContext.action.name);
    public void BeginActionEvent(string name) => StateMachine.SendSignal(name);

    public void ReadyNextAction() => StateMachine.ReadySignal();
    public void FinishAction() => StateMachine.FinishSignal();



    public void ParryAction()
    {
        if(interacter.TryInteract()) return;

        //Do Parry move here.
    }

    //Other events.
    private void JumpRelease(CTX ctx) => StateMachine.SendSignal("JumpRelease", false, true);
    private void ShootModeActivate(CTX ctx) => StateMachine.SendSignal("ShootMode", true, true);
    private void ShootModeDeactivate(CTX ctx) => StateMachine.SendSignal("ShootModeExit", true, true);




}