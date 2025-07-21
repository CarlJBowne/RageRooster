using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;
using static TrackerEB;

public class PlayerAirborneMovement : PlayerMovementEffector
{

    [Header("Horizontal")]
    public float acceleration;
    public float decceleration;
    public float maxSpeed;
    public float stopping = 0.75f;
    [Tooltip("1 = full second turn, 50 = 1 FixedUpdate turn")]
    public float maxTurnSpeed = 25;
    public bool isDash;
    public float minSpeed;
    [Header("Vertical")]

    public int defaultPhase;
    public float gravity = 9.81f;
    public float terminalVelocity = 100f;
    public bool flatGravity = false;
    public PlayerAirborneMovement fallState;
    public float fallStateThreshold = 0;


    [ToggleGroup("Upwards", nameof(jumpHeight), nameof(jumpPower), nameof(jumpMinHeight), nameof(allowMidFall))]
    public bool upwards;
    [HideProperty] public float jumpHeight;
    [HideProperty] public float jumpPower;
    [HideProperty] public float jumpMinHeight;
    [HideField(nameof(upwards))] public bool forceDownwards;
    [HideProperty, ShowField(nameof(upwards))] public bool allowMidFall = true;

    protected float targetMinHeight;
    protected float targetHeight;



    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float currentSpeed = playerMovementBody.CurrentSpeed;
        Vector3 currentDirection = playerMovementBody.currentDirection;

        if (!isDash) HorizontalMain (ref currentSpeed, ref currentDirection, playerController.camAdjustedMovement, Time.fixedDeltaTime * 50);
        else HorizontalCharge       (ref currentSpeed, ref currentDirection, playerController.camAdjustedMovement, Time.fixedDeltaTime * 50);

        playerMovementBody.CurrentSpeed = currentSpeed;

        Vector3 literalDirection = transform.forward * currentSpeed;

        resultX = literalDirection.x;
        resultZ = literalDirection.z;
    }
    public override void VerticalMovement(out float? result)
    {
        result = ApplyGravity(gravity, terminalVelocity, flatGravity);
        if (upwards) VerticalUpwards(ref result);
        else if (playerMovementBody.velocity.y <= fallStateThreshold) Fall(ref result);
    }

    private void HorizontalMain(ref float currentSpeed, ref Vector3 currentDirection, Vector3 control, float deltaTime)
    {
        Vector3 controlDirection = control.normalized;
        float controlMag = control.magnitude;

        if (controlMag > 0)
        {
            float Dot = Vector3.Dot(controlDirection, currentDirection);

            if (maxTurnSpeed > 0) playerMovementBody.DirectionSet(maxTurnSpeed);

            currentSpeed *= Dot;
            if (currentSpeed < maxSpeed)
                currentSpeed = currentSpeed.MoveUp(controlMag * acceleration * deltaTime, maxSpeed);
            else if (currentSpeed > maxSpeed)
                currentSpeed = currentSpeed.MoveDown(controlMag * decceleration * deltaTime, maxSpeed);

        }
        else currentSpeed = currentSpeed > .01f ? currentSpeed.MoveTowards(currentSpeed * stopping * deltaTime, 0) : 0;

    }
    private void HorizontalCharge(ref float currentSpeed, ref Vector3 currentDirection, Vector3 control, float deltaTime)
    {
        Vector3 controlDirection = control.normalized;
        float controlMag = control.magnitude;


        if (controlMag > 0.1f)
        {
            if (currentSpeed < maxSpeed)
                currentSpeed = currentSpeed.MoveUp(controlMag * acceleration * deltaTime, maxSpeed);
        }
        else
        {
            if (currentSpeed < minSpeed)
                currentSpeed = currentSpeed.MoveUp(controlMag * acceleration * deltaTime, minSpeed);
            if (currentSpeed > minSpeed)
                currentSpeed = currentSpeed.MoveDown(controlMag * decceleration * deltaTime, maxSpeed);
        }

        if (maxTurnSpeed > 0) playerMovementBody.DirectionSet(maxTurnSpeed);
        playerMovementBody.CurrentSpeed = currentSpeed; 


    }


    private void VerticalUpwards(ref float? Y)
    {
        if (playerMovementBody.jumpPhase == 0 && transform.position.y >= targetMinHeight) playerMovementBody.jumpPhase = 1;
        if (playerMovementBody.jumpPhase == 1 && transform.position.y >= targetHeight) playerMovementBody.jumpPhase = 2;

        if (playerMovementBody.jumpPhase < 2) Y = jumpPower;
        if (playerMovementBody.jumpPhase > 0 && 
           (playerMovementBody.velocity.y <= fallStateThreshold || (allowMidFall && !Input.Jump.IsPressed())))
           Fall(ref Y);

    }

    private void Fall(ref float? Y)
    {
        if (playerMovementBody.velocity.y > fallStateThreshold) Y = fallStateThreshold;
        playerMovementBody.jumpPhase = 3;
        if (fallState != null) fallState.Enter();
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        if (!isFinal) return;

        playerMovementBody.GroundStateChange(false);

        int nextJumpPhase = defaultPhase;
        if(nextJumpPhase == -1)
        {
            nextJumpPhase = playerMovementBody.jumpPhase;
            if (nextJumpPhase == -1) nextJumpPhase = 0;
        }

        playerMovementBody.jumpPhase = nextJumpPhase;
        switch (nextJumpPhase)
        {
            case 0: StartFrom0(); break;
            case 1: StartFrom1(); break;
            case 2: StartFrom2(); break;
            case 3: StartFrom3(); break;
        }

        void StartFrom0()
        {
            if (isDash) playerMovementBody.CurrentSpeed = maxSpeed;

            if (!upwards) return;

            playerMovementBody.VelocitySet(y: jumpPower);
            targetMinHeight = transform.position.y + jumpMinHeight;
            targetHeight = (transform.position.y + jumpHeight) - (jumpPower.P()) / (2 * gravity);
            if (targetHeight <= transform.position.y)
            {
                playerMovementBody.VelocitySet(y: Mathf.Sqrt(2 * gravity * jumpHeight));
                targetMinHeight = transform.position.y;
            }

            #if UNITY_EDITOR
            playerMovementBody.jumpMarkers = new()
                {
                    transform.position,
                    transform.position + Vector3.up * targetHeight,
                    transform.position + Vector3.up * jumpHeight
                };
            #endif
        }
        void StartFrom1()
        {

        }
        void StartFrom2()
        {

        }
        void StartFrom3()
        {
            playerMovementBody.VelocitySet(y: playerMovementBody.velocity.y.Max(0));
        }
    }
    //public override void OnExit(State next) => body.jumpPhase = -1;

    public void Enter() => state.TransitionTo();
    public void BeginJump()
    {
        playerMovementBody.GroundStateChange(false);
        if(!state) state.TransitionTo();
    }
    public void BeginJump(float power, float height, float minHeight)
    {
        if (!upwards) throw new System.Exception("This isn't an Upward Item.");
        jumpPower = power;
        jumpHeight = height;
        jumpMinHeight = minHeight;

        state.TransitionTo();
    }
}
