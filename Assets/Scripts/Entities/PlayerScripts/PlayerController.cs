using SLS.StateMachineV3;
using UnityEngine;
using System.Linq;
using EditorAttributes;
using UnityEngine.InputSystem;
using Cinemachine;
using System.Collections.Generic;
using PCA = PlayerControlAction;

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

	[HideInInspector] public float jumpInput;
	[HideInInspector] public Vector3 camAdjustedMovement;
	[HideInInspector] public PlayerRanged grabber;

	#endregion
	#region Getters

	#endregion

	public override void OnAwake()
	{
		grabber = GetComponentFromMachine<PlayerRanged>();
		//attack = GetComponentFromMachine<PlayerAttackSystem>();

		input.jump.performed            += BeginActionEvent;
        input.attackTap.performed       += BeginActionEvent;
        input.attackHold.performed      += BeginActionEvent;
        input.grabTap.performed         += BeginActionEvent;
        input.grabHold.performed        += BeginActionEvent;
        input.parry.performed           += BeginActionEvent;
        input.chargeTap.performed       += BeginActionEvent;
        input.chargeHold.performed      += BeginActionEvent;
        input.shoot.performed           += BeginActionEvent;
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
    }

    public override void OnUpdate()
	{

		if (jumpInput > 0) jumpInput -= Time.deltaTime;
		camAdjustedMovement = input.movement.ToXZ().Rotate(M.cameraTransform.eulerAngles.y, Vector3.up);

		if (M.signalReady && input.jump.IsPressed() && sFall && !grabber.currentGrabbed) 
            sGlide.TransitionTo();
        else if(M.signalReady && !input.jump.IsPressed() && sGlide) 
            sFall.TransitionTo();

        if (M.freeLookCamera != null)
        {
            M.freeLookCamera.Follow = transform;
            M.freeLookCamera.LookAt = transform;
        }

        if (input.shootMode.IsPressed() && M.signalReady && sGrounded 
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


    private void BeginActionEvent(InputAction.CallbackContext callbackContext) => M.SendSignal(callbackContext.action.name);
    public void BeginActionEvent(string name) => M.SendSignal(name);

    public void ReadyNextAction() => M.ReadySignal();
    public void FinishAction() => M.FinishSignal();



    public void ParryAction()
    {
        var interactCheck = Physics.OverlapSphere(body.center + body.transform.forward * 2, 1.5f);
        for (int i = 0; i < interactCheck.Length; i++)
            if (interactCheck[i].TryGetComponent(out IInteractable foundInteractable))
            {
                foundInteractable.Interact();
                return;
            }

        //Do Parry move here.
    }


}
