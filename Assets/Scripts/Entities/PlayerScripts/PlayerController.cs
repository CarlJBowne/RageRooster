using SLS.StateMachineV2;
using UnityEngine;
using System.Linq;
using EditorAttributes;
using UnityEngine.InputSystem;

public class PlayerController : PlayerStateBehavior
{
	#region Config

	public float jumpBuffer = 0.3f;

	public State groundedState;
	public State idleWalkState;
	public State chargingState;
	public State airborneState;
	public State fallingState;
	public State glidingState;
	public State groundSlamState;
	public PlayerWallJump wallJumpState;
    public PlayerAirborn airChargeState;
    public PlayerAirborn airChargeFallState;

    public string punchAnimName;

	#endregion
	#region Data

	[HideInInspector] public float jumpInput;
	[HideInInspector] public Vector3 camAdjustedMovement;
	[HideInInspector] public PlayerGrabber grabber;
	//[HideInInspector] public PlayerAttackSystem attack;

	#endregion
	#region Getters

	#endregion

	public override void OnAwake()
	{
		grabber = GetComponentFromMachine<PlayerGrabber>();
		//attack = GetComponentFromMachine<PlayerAttackSystem>();

		input.jump.started += _ => JumpPress();
		input.grab.started += _ => grabber.GrabButtonPress();
		input.attack.started += _ => PunchButtonPress();
		input.attack.started += _ => ChargeButtonPress();
	}

	private void OnDestroy()
	{
		input.jump.started -= _ => JumpPress();
		input.grab.started -= _ => grabber.GrabButtonPress();
		input.attack.started -= _ => PunchButtonPress();
		input.attack.started -= _ => ChargeButtonPress();
	}

	public override void OnUpdate()
	{
		if (jumpInput > 0) jumpInput -= Time.deltaTime;
		camAdjustedMovement = input.movement.ToXZ().Rotate(M.cameraTransform.eulerAngles.y, Vector3.up);

		if ((input.jump.WasPressedThisFrame() && fallingState.active && !grabber.currentGrabbed) || (!input.jump.WasReleasedThisFrame() && glidingState.active))
			TransitionTo(input.jump.IsPressed() ? glidingState : fallingState);
		M.animator.SetBool("Gliding", glidingState.active);

		if (input.charge.IsPressed() && groundedState.active || !input.charge.IsPressed() && chargingState.active)
			TransitionTo(input.charge.IsPressed() ? chargingState : idleWalkState);

        if (input.charge.WasPressedThisFrame() && airborneState && !airChargeState.state && !airChargeFallState.state)
        {
            airChargeState.BeginJump();
            body.currentSpeed = airChargeState.state.Behavior<PlayerDirectionalMovement>().maxSpeed;
            body.currentDirection = controller.camAdjustedMovement.magnitude > 0.1f ? controller.camAdjustedMovement : transform.forward;
        }
    }

	private void JumpPress()
	{
		if (groundedState.active || (airborneState.active && body.coyoteTimeLeft > 0)) body.BeginJump();
		else
		{
			jumpInput = jumpBuffer + Time.fixedDeltaTime;

			if (M.WallJump && (fallingState.active || wallJumpState.state.active)
				&& body.rb.DirectionCast(body.currentDirection, 0.5f, body.checkBuffer, out RaycastHit hit))
				wallJumpState.WallJump(hit.normal); 
		} 
		
	}

	public bool CheckJumpBuffer()
	{
		bool result = jumpInput > 0;
		jumpInput = 0;
		return result;
	}

	public void PunchButtonPress()
	{
		if (airborneState.active && M.GroundSlam) groundSlamState.TransitionTo();
		else M.animator.Play(punchAnimName);
	}

	public void ChargeButtonPress()
	{
		if (airborneState.active)
		{

		}
	}

}
