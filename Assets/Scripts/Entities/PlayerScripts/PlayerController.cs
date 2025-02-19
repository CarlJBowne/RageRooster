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
    public PlayerAiming aimingState;
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

		if (readyForNextAction && input.jump.IsPressed() && sFall && !grabber.currentGrabbed) 
            sGlide.TransitionTo();
        else if(readyForNextAction && !input.jump.IsPressed() && sGlide) 
            sFall.TransitionTo();

        if (M.freeLookCamera != null)
        {
            M.freeLookCamera.Follow = transform;
            M.freeLookCamera.LookAt = transform;
        }

        if(input.shootMode.IsPressed() && sGrounded && readyForNextAction) aimingState.EnterMode();
    }

    public bool CheckJumpBuffer()
    {
        bool result = jumpInput > 0;
        jumpInput = 0;
        return result;
    }
    public void BeginJumpInputBuffer() => jumpInput = jumpBuffer + Time.fixedDeltaTime;




    public PCA currentAction;
    [HideProperty]
    public bool readyForNextAction = true;
    public Queue<InputActionReference> actionQueue = new();

    private void BeginActionEvent(InputAction.CallbackContext callbackContext)
    {
        if (PauseMenu.Active) return;
        InputActionReference action = callbackContext.action.Reference();
        if (!readyForNextAction || currentAction == null || !currentAction.TryNextAction(action))
        {
            actionQueue.Enqueue(action);
            inputQueueDecay.Begin();
        }
    }






    public void ReadyNextAction()
    {
        readyForNextAction = true;
        if(actionQueue.Count > 0)
        {
            if(currentAction.HasAction(actionQueue.Peek())) currentAction.TryNextAction(actionQueue.Dequeue());
            else
            {
                actionQueue.Dequeue();
                while(actionQueue.Count > 0)
                {
                    if (currentAction.HasAction(actionQueue.Peek()))
                    {
                        currentAction.TryNextAction(actionQueue.Dequeue());
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
