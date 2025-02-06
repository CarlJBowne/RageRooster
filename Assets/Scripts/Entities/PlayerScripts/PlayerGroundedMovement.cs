using EditorAttributes;
using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundedMovement : PlayerMovementEffector
{
    [Header("Horizontal")]
    public float acceleration;
    public float decceleration;
    public float maxSpeed;
    public float stopping = 0.75f;
    [Tooltip("1 = full second turn, 50 = 1 FixedUpdate turn")]
    public float maxTurnSpeed = 25;
    public bool outwardTurn;
    public float minSpeed;
    public PlayerGroundedMovement prevPhase;
    [ShowField(nameof(__hasPrevPhase))] public float prevPhaseThreshold;
    public PlayerGroundedMovement nextPhase;
    [ShowField(nameof(__hasNextPhase))] public float nextPhaseThreshold;
    [ShowField(nameof(__hasNextPhase))] public bool needChargeButton;
    [ShowField(nameof(__hasNextPhase))] public bool needRagingUpgrade;

    #region Editor
    private bool __hasPrevPhase => prevPhase != null;
    private bool __hasNextPhase => nextPhase!= null;
    #endregion

    private Collider attackCollider;

    public override void OnAwake() => attackCollider = GetComponent<Collider>();

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float currentSpeed = body.currentSpeed;
        Vector3 currentDirection = body.currentDirection;

        HorizontalMain(ref currentSpeed, ref currentDirection, controller.camAdjustedMovement);

        body.currentDirection = currentDirection;
        body.currentSpeed = currentSpeed;

        Vector3 literalDirection = transform.forward * currentSpeed;

        resultX = literalDirection.x;
        resultZ = literalDirection.z;

    }

    private void HorizontalMain(ref float currentSpeed, ref Vector3 currentDirection, Vector3 control)
    {
        float deltaTime = Time.fixedDeltaTime / 0.02f;
        Vector3 controlDirection = control.normalized;
        float controlMag = control.magnitude;
        float speedAlter = 0f;

        if (controlMag > 0)
        {
            float Dot = Vector3.Dot(controlDirection, currentDirection);

            if (maxTurnSpeed > 0)
                currentDirection = Vector3.RotateTowards(currentDirection, controlDirection, maxTurnSpeed * Mathf.PI * Time.fixedDeltaTime, 0);

            if (!outwardTurn) currentSpeed *= Dot;
            if (currentSpeed > maxSpeed)
                speedAlter = (controlMag * -decceleration).Min(maxSpeed) * deltaTime;
            else if (currentSpeed < maxSpeed)
                speedAlter = (controlMag * acceleration).Max(maxSpeed) * deltaTime;
            else if (Condition() && currentSpeed < nextPhase.maxSpeed)
                speedAlter = (controlMag * nextPhase.acceleration).Max(nextPhase.maxSpeed) * deltaTime;
        }
        else speedAlter = -stopping * deltaTime;

        currentSpeed += speedAlter;

        if (speedAlter > 0 && currentSpeed >= nextPhaseThreshold && Condition()) nextPhase.state.TransitionTo();
        else if (speedAlter < 0 && currentSpeed < prevPhaseThreshold) prevPhase.state.TransitionTo();

    }
    
    private bool Condition() => (!needChargeButton || Input.ChargeHold.IsPressed()) && (!needRagingUpgrade || controller.ragingChargeUpgrade);

    public override void OnEnter(State prev){if(attackCollider != null) attackCollider.enabled = true;}
    public override void OnExit(State next){if(attackCollider == null) attackCollider.enabled = false;}
}

public abstract class PlayerMovementEffector : PlayerStateBehavior
{
    [HideInEditMode, DisableInPlayMode] public bool trueActive;

    public override void OnFixedUpdate()
    {
        if (!trueActive) return;
        this.HorizontalMovement(out float? X, out float? Z);
        this.VerticalMovement(out float? Y);
        body.VelocitySet(X, Y, Z);
    }

    public virtual void HorizontalMovement(out float? resultX, out float? resultZ) { resultX = null; resultZ = null; }
    public virtual void VerticalMovement(out float? result) { result = null; }

    protected float ApplyGravity(float gravity, float terminalVelocity, bool flatGravity = false)
    {
        return (!flatGravity
            ? body.velocity.y - (gravity * Time.deltaTime)
            : -gravity * Time.deltaTime
            ).Min(-terminalVelocity);
    }

    public override void OnEnter(State prev) => trueActive = state.ActiveMain();
}