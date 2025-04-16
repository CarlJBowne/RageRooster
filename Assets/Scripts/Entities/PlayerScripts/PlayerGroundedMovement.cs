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
    
    [FoldoutGroup("Conditions", nameof(needs1Charge), nameof(needs2Charge), nameof(needsRagingUpgrade))]
    public Void lifetimeEventsHolder;

    [SerializeField, HideInInspector] public bool needs1Charge;
    [SerializeField, HideInInspector] public bool needs2Charge;
    [SerializeField, HideInInspector] public bool needsRagingUpgrade;

    #region Editor
    private bool __hasPrevPhase => prevPhase != null;
    private bool __hasNextPhase => nextPhase!= null;
    #endregion

    private Collider attackCollider;

    public override void OnAwake() => attackCollider = GetComponent<Collider>();

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float currentSpeed = playerMovementBody.CurrentSpeed;
        Vector3 currentDirection = playerMovementBody.currentDirection;

        HorizontalMain(ref currentSpeed, currentDirection, playerController.camAdjustedMovement);

        playerMovementBody.CurrentSpeed = currentSpeed;

        Vector3 literalDirection = transform.forward * currentSpeed;

        resultX = literalDirection.x;
        resultZ = literalDirection.z;

    }

    private void HorizontalMain(ref float currentSpeed, Vector3 currentDirection, Vector3 control)
    {
        float deltaTime = Time.deltaTime * 50;
        Vector3 controlDirection = control.normalized;
        float controlMag = control.magnitude;

        GetConditionals(out bool thisCondition, out bool nextCondition);

        if (controlMag > 0)
        {
            float Dot = Vector3.Dot(controlDirection, currentDirection);

            if (maxTurnSpeed > 0) playerMovementBody.DirectionSet(maxTurnSpeed); 

            if (!outwardTurn) currentSpeed *= Dot;

            //This ordering is weird, but important.
            if (nextCondition && currentSpeed < nextPhase.maxSpeed)
                currentSpeed = currentSpeed.MoveUp(controlMag * nextPhase.acceleration * deltaTime, nextPhase.maxSpeed);

            else if (!thisCondition)
                currentSpeed = currentSpeed.MoveDown(decceleration * deltaTime, prevPhase.maxSpeed);

            else if (currentSpeed < maxSpeed)
                currentSpeed = currentSpeed.MoveUp(controlMag * acceleration * deltaTime, maxSpeed);

            else if (currentSpeed > maxSpeed)
                currentSpeed = currentSpeed.MoveDown(decceleration * deltaTime, maxSpeed);
        }
        else currentSpeed = currentSpeed > .01f ? currentSpeed.MoveTowards(currentSpeed * stopping * deltaTime, 0) : 0;

        if (currentSpeed >= nextPhaseThreshold && nextCondition) nextPhase.state.TransitionTo();
        else if (currentSpeed < prevPhaseThreshold && prevPhase != null) prevPhase.state.TransitionTo();
    }
    
    private void GetConditionals(out bool thisCondition, out bool nextCondition)
    {
        thisCondition = 
            (!needs1Charge || Input.Charge1.IsPressed() || Input.Charge2.IsPressed()) &&      
            (!needs2Charge || (Input.Charge1.IsPressed() && Input.Charge2.IsPressed())) &&     
            (!needsRagingUpgrade || playerController.ragingChargeUpgrade)           
            ;

        nextCondition = nextPhase != null &&
            (!nextPhase.needs1Charge || Input.Charge1.IsPressed() || Input.Charge2.IsPressed()) &&
            (!nextPhase.needs2Charge || (Input.Charge1.IsPressed() && Input.Charge2.IsPressed())) &&
            (!nextPhase.needsRagingUpgrade || playerController.ragingChargeUpgrade)
            ;
    }

    public override void OnEnter(State prev, bool isFinal){ base.OnEnter(prev, isFinal); if (attackCollider != null) attackCollider.enabled = true;}
    public override void OnExit(State next){if(attackCollider != null) attackCollider.enabled = false;}
}
