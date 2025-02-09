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
    public PlayerAirborneMovement airChargeState;
    public PlayerAirborneMovement airChargeFallState;
	public Upgrade groundSlamUpgrade;
	public Upgrade wallJumpUpgrade;
    public Upgrade ragingChargeUpgrade;
    public Timer.OneTime inputQueueDecay = new(1f);

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

		input.jump.performed            += BeginActionEvent;
        input.attackTap.performed       += BeginActionEvent;
        input.attackHold.performed      += BeginActionEvent;
        input.grabTap.performed         += BeginActionEvent;
        input.grabHold.performed        += BeginActionEvent;
        input.parry.performed           += BeginActionEvent;
        input.chargeTap.performed       += BeginActionEvent;
        input.chargeHold.performed      += BeginActionEvent;
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
    }

    public override void OnUpdate()
	{
        if (inputQueueDecay.running) inputQueueDecay.Tick(() =>
        {
            if (actionQueue.Count > 0) actionQueue.Dequeue();
            if (actionQueue.Count > 0) inputQueueDecay.Begin();
        });

		if (jumpInput > 0) jumpInput -= Time.deltaTime;
		camAdjustedMovement = input.movement.ToXZ().Rotate(M.cameraTransform.eulerAngles.y, Vector3.up);

		if ((input.jump.IsPressed() && sFall && !grabber.currentGrabbed) || (!input.jump.IsPressed() && sGlide))
        {
            TransitionTo(input.jump.IsPressed() ? sGlide : sFall);
            M.animator.SetBool("Gliding", sGlide);
        }
			

		//if (input.chargeTap.IsPressed() && sGrounded || !input.chargeTap.IsPressed() && sCharge)
		//	TransitionTo(input.chargeTap.IsPressed() ? sCharge : sIdleWalk);

        //if (input.chargeTap.WasPressedThisFrame() && sAirborne && !airChargeState.state && !airChargeFallState.state)
        //{
        //    airChargeState.BeginJump();
        //    body.currentSpeed = airChargeState.state.Behavior<PlayerDirectionalMovement>().maxSpeed;
        //    body.currentDirection = controller.camAdjustedMovement.magnitude > 0.1f ? controller.camAdjustedMovement : transform.forward;
        //}

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





    public PCA currentAction;
    [HideProperty]
    public bool readyForNextAction = true;
    public Queue<InputActionReference> actionQueue = new();

    private void BeginActionEvent(InputAction.CallbackContext callbackContext)
    {
        InputActionReference action = callbackContext.action.Reference();
        if (readyForNextAction && currentAction.IsActionValid(action)) BeginAction(action);
        else
        {
            actionQueue.Enqueue(action);
            inputQueueDecay.Begin();
        }
    }






    private void BeginAction(InputActionReference action) => currentAction.feedingActions[action]?.Invoke();

    public void ReadyNextAction()
    {
        readyForNextAction = true;
        if(actionQueue.Count > 0)
        {
            if(currentAction.IsActionValid(actionQueue.Peek())) BeginAction(actionQueue.Dequeue());
            else
            {
                actionQueue.Dequeue();
                while(actionQueue.Count > 0)
                {
                    if (currentAction.IsActionValid(actionQueue.Peek()))
                    {
                        BeginAction(actionQueue.Dequeue());
                        break;
                    }
                    else actionQueue.Dequeue();
                }
            }
        }
    }
    public void FinishAction()
    {
        if (readyForNextAction && currentAction != null) currentAction.Finish();
        else if (M.currentState.TryGetComponent(out PlayerStateAnimator anim)) anim.Finish();
    }


    public void JumpAction()
    {
        if (sGrounded || (sAirborne && body.coyoteTimeLeft > 0)) body.BeginJump();
        else jumpInput = jumpBuffer + Time.fixedDeltaTime;

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
