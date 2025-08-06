using SLS.StateMachineH;
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

    public bool overrideMovementControl;
    public Vector2 overrideMovementVector;

	#endregion
	#region Data

	[HideProperty] public float jumpInput;
	[HideProperty] public Vector3 camAdjustedMovement;
	[HideProperty] public PlayerRanged grabber;
    [HideProperty] public PlayerInteracter interacter;

    #endregion
    #region Getters

    #endregion

    protected override void OnAwake()
	{
		if(!grabber) grabber = GetComponentFromMachine<PlayerRanged>();
        if(!interacter) interacter = GetComponentFromMachine<PlayerInteracter>();

		Input.Jump.performed            += JumpPress;
        Input.AttackTap.performed       += BeginActionEvent;
        Input.AttackHold.performed      += BeginActionEvent;
        Input.Grab.performed         += BeginActionEvent;
        Input.Parry.performed           += BeginActionEvent;
        Input.Interact.performed        += BeginActionEvent;

        Input.Jump.canceled             += JumpRelease;
        Input.Aim.performed       += ShootModeActivate;
        Input.Aim.canceled        += ShootModeDeactivate;

        Input.Charge1.performed       += ChargeButtons;
        Input.Charge2.performed       += ChargeButtons;
    }


	private void OnDestroy()
	{
        Input.Jump.performed -= BeginActionEvent;
        Input.AttackTap.performed -= BeginActionEvent;
        Input.AttackHold.performed -= BeginActionEvent;
        Input.Grab.performed -= BeginActionEvent;
        Input.Parry.performed -= BeginActionEvent;
        Input.Interact.performed -= BeginActionEvent;

        Input.Jump.canceled -= JumpRelease;
        Input.Aim.performed -= ShootModeActivate;
        Input.Aim.canceled -= ShootModeDeactivate;

        Input.Charge1.performed -= ChargeButtons;
        Input.Charge2.performed -= ChargeButtons;

    }

    protected override void OnUpdate()
	{

		if (jumpInput > 0) jumpInput -= Time.deltaTime;
		if(!overrideMovementControl) camAdjustedMovement = Input.Movement.ToXZ().Rotate(Machine.cameraTransform.eulerAngles.y, Vector3.up);
		else camAdjustedMovement = overrideMovementVector.ToXZ().Rotate(Machine.cameraTransform.eulerAngles.y, Vector3.up);

		//if (Machine.signalReady && input.jump.IsPressed() && sFall && !grabber.currentGrabbed) 
        //    sGlide.Enter();
        //else if(Machine.signalReady && !input.jump.IsPressed() && sGlide) 
        //    sFall.Enter();

        if (Machine.freeLookCamera != null)
        {
            Machine.freeLookCamera.Follow = transform;
            Machine.freeLookCamera.LookAt = transform;
        }

        //if (Input.Aim.IsPressed() && Machine.signalReady && sGrounded 
        //    && !ranged.aimingState && (ranged.hasEggsToShoot || grabber.currentGrabbed != null)) 
        //    ranged.EnterAiming();
    }

    public bool CheckJumpBuffer()
    {
        bool result = jumpInput > 0;
        jumpInput = 0;
        return result;
    }
    public void BeginJumpInputBuffer() => jumpInput = jumpBuffer + Time.fixedDeltaTime;



    private void BeginActionEvent(InputAction.CallbackContext callbackContext) => Machine.SendSignal(callbackContext.action.name);
    public void BeginActionEvent(string name) => Machine.SendSignal(name);

    public void ReadyNextAction() => Machine.SignalManager.Unlock();
    public void FinishAction() => Machine.SignalManager.FireSignal(new("Finish", ignoreLock: true));



    public void ParryActionAirborne()
    {
        if(hellcopterUpgrade)
        {
            airSpin.Enter();
            if (playerMovementBody.isOverVent) Machine.SendSignal(new("EnterVent", 0, true));
        }
    }

    public void MidJumpJumpAction()
    {
        if (!wallJumpState.WallJump(transform.forward))
        {
            (!playerMovementBody.isOverVent ? sGlide : ventGlideState).Enter();
        }
    }
    public void MidWallJumpJumpAction() => wallJumpState.WallJump(transform.forward);

    //Other events.
    private void JumpPress(CTX ctx) => jumpInput = Machine.SendSignal(ctx.action.name) ? 0 : jumpBuffer;
    private void JumpRelease(CTX ctx) => Machine.SendSignal(new("JumpRelease", 0, true));
    private void ShootModeActivate(CTX ctx) => Machine.SendSignal(new("ShootMode", ignoreLock:true));
    private void ShootModeDeactivate(CTX ctx) => Machine.SendSignal(new("ShootModeExit", ignoreLock: true));

    private void ChargeButtons(CTX ctx) => Machine.SendSignal("Charge");


}