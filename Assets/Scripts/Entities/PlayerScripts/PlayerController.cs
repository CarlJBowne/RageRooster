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

	//public State groundedState;
	//public State idleWalkState;
	//public State chargingState;
	//public State airborneState;
	//public State fallingState;
	//public State glidingState;
	//public State groundSlamState;
	public PlayerWallJump wallJumpState;
    public PlayerAirborn airChargeState;
    public PlayerAirborn airChargeFallState;
	public Upgrade groundSlamUpgrade;
	public Upgrade wallJumpUpgrade;

    public string punchTriggerName;

	#endregion
	#region Data

	[HideInInspector] public float jumpInput;
	[HideInInspector] public Vector3 camAdjustedMovement;
	[HideInInspector] public PlayerGrabber grabber;

	#endregion
	#region Getters

	#endregion

	public override void OnAwake()
	{
		grabber = GetComponentFromMachine<PlayerGrabber>();
		//attack = GetComponentFromMachine<PlayerAttackSystem>();

		input.jump.started += BeginActionEvent;
        input.grabTap.started += BeginActionEvent;
        input.attackTap.started += BeginActionEvent;
        input.parry.started += BeginActionEvent;
    }

	private void OnDestroy()
	{
		input.jump.started -= BeginActionEvent;
        input.grabTap.started -= BeginActionEvent;
        input.attackTap.started -= BeginActionEvent;
        input.parry.started -= BeginActionEvent;
    }

	public override void OnUpdate()
	{
		if (jumpInput > 0) jumpInput -= Time.deltaTime;
		camAdjustedMovement = input.movement.ToXZ().Rotate(M.cameraTransform.eulerAngles.y, Vector3.up);

		if ((input.jump.WasPressedThisFrame() && sFall && !grabber.currentGrabbed) || (!input.jump.WasReleasedThisFrame() && sGlide))
			TransitionTo(input.jump.IsPressed() ? sGlide : sFall);
		M.animator.SetBool("Gliding", sGlide);

		if (input.chargeTap.IsPressed() && sGrounded || !input.chargeTap.IsPressed() && sCharge)
			TransitionTo(input.chargeTap.IsPressed() ? sCharge : sIdleWalk);

        if (input.chargeTap.WasPressedThisFrame() && sAirborne && !airChargeState.state && !airChargeFallState.state)
        {
            airChargeState.BeginJump();
            body.currentSpeed = airChargeState.state.Behavior<PlayerDirectionalMovement>().maxSpeed;
            body.currentDirection = controller.camAdjustedMovement.magnitude > 0.1f ? controller.camAdjustedMovement : transform.forward;
        }

		if (M.freeLookCamera != null)
        {
            M.freeLookCamera.Follow = transform;
            M.freeLookCamera.LookAt = transform;
        }
    }

    public bool CheckJumpBuffer()
    {
        bool result = jumpInput > 0;
        jumpInput = 0;
        return result;
    }





    private PCA currentAction;
    private bool readyForNextAction = true;
	private Queue<PCA> actionQueue;

    private void BeginActionEvent(InputAction.CallbackContext callbackContext)
    {
        if(currentAction.feedingActions.TryGetValue(callbackContext.action, out PCA nextAct) &&
            (nextAct.necessaryUpgrade == null || nextAct.necessaryUpgrade)
            )
            if (readyForNextAction) BeginAction(nextAct);
            else actionQueue.Enqueue(nextAct);
    }





    public void ApplyCurrentAction(PCA action) => currentAction = action;

    private void BeginAction(PCA action)
	{

    }
    private void EndAction()
    {
        actionQueue.Dequeue();
        if (actionQueue.Count > 0) DoAction(actionQueue.Peek());
    }
    private void DoAction(InputAction action)
	{
        if (action == input.jump) JumpAction();
        else if (action == input.attackTap) AttackAction(false);
        else if (action == input.attackHold) AttackAction(true);
        else if (action == input.grabTap) GrabAction(false);
        else if (action == input.grabHold) GrabAction(true);
        else if (action == input.parry) ParryAction();
        else if (action == input.chargeTap) ChargeAction(false);
        else if (action == input.chargeHold) ChargeAction(true);
    }

    private void JumpAction()
    {
        if (sGrounded || (sAirborne && body.coyoteTimeLeft > 0)) body.BeginJump();
        else
        {
            jumpInput = jumpBuffer + Time.fixedDeltaTime;

            if (wallJumpUpgrade && (sFall || wallJumpState)
                && body.rb.DirectionCast(body.currentDirection, 0.5f, body.checkBuffer, out RaycastHit hit))
                wallJumpState.WallJump(hit.normal);
        }

    }

    public void AttackAction(bool held)
    {
        if (sAirborne && groundSlamUpgrade) sGroundSlam.TransitionTo();
        else M.animator.SetTrigger(punchTriggerName);
    }

    public void GrabAction(bool held)
    {

    }

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

    public void ChargeAction(bool held)
    {

    }












}
