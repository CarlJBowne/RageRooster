using EditorAttributes;
using RageRooster.Systems.SaveSystem;
using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static SLS.StateMachineH.StateAnimator;
using static UnityEngine.EventSystems.EventTrigger;

[System.Obsolete("This is an old script, Use PlayerGroundMovement instead.")]
public class PlayerGroundMovementComplex : PlayerMovementEffector
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
    public PlayerGroundMovementComplex rollState;
    public PlayerGroundMovementComplex walkState;
    public PlayerGroundMovementComplex prevPhase;
    [ShowField(nameof(__hasPrevPhase))] public float prevPhaseThreshold;
    public PlayerGroundMovementComplex nextPhase;
    [ShowField(nameof(__hasNextPhase))] public float nextPhaseThreshold;
    
    [FoldoutGroup("Conditions", nameof(needs1Charge), nameof(needs2Charge), nameof(needsRagingUpgrade), nameof(canRoll))]
    public Void lifetimeEventsHolder;

    [SerializeField, HideInInspector] public bool needs1Charge;
    [SerializeField, HideInInspector] public bool needs2Charge;
    [SerializeField, HideInInspector] public bool needsRagingUpgrade;
    [SerializeField, HideInInspector] public bool canRoll;

    #region Editor
    private bool __hasPrevPhase => prevPhase != null;
    private bool __hasNextPhase => nextPhase!= null;
    #endregion

    private Collider attackCollider;

    protected override void OnAwake() => attackCollider = GetComponent<Collider>();

    /*
    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float currentSpeed = Player.MovementBody.velocity.f;
        Vector3 currentDirection = Player.MovementBody.DirectionGet;

        HorizontalMain(ref currentSpeed, currentDirection, Player.Controller.camAdjustedMovement);

        Player.MovementBody.velocity.f = currentSpeed;

        Vector3 literalDirection = transform.forward * currentSpeed;

        resultX = literalDirection.x;
        resultZ = literalDirection.z;

    }
    */

    private void HorizontalMain(ref float currentSpeed, Vector3 currentDirection, Vector3 control)
    {
        float deltaTime = Time.deltaTime * 50;
        Vector3 controlDirection = control.normalized;
        float controlMag = control.magnitude;

        GetConditionals(out bool thisCondition, out bool nextCondition);

        if (controlMag > 0)
        {
            float Dot = Vector3.Dot(controlDirection, currentDirection);

            if (maxTurnSpeed > 0) Player.MovementBody.DirectionSet(maxTurnSpeed * Time.fixedDeltaTime);

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
        else currentSpeed = currentSpeed > .01f ? currentSpeed.Move(currentSpeed * stopping * deltaTime, 0) : 0;

        if (currentSpeed >= nextPhaseThreshold && nextCondition)
            nextPhase.State.Enter();
        else if (currentSpeed < prevPhaseThreshold && prevPhase != null)
            prevPhase.State.Enter();

        if (currentSpeed >= 12)
            canRoll = true;

    }
    
    private void GetConditionals(out bool thisCondition, out bool nextCondition)
    {
        thisCondition = 
            //(!needs1Charge || Input.Charge1.IsPressed() || Input.Charge2.IsPressed()) &&     
            (!needs2Charge || (Input.Charge1.IsPressed() && Input.Charge2.IsPressed())) &&     
            (!needsRagingUpgrade || Upgrades.Active.ragingCharge)           
            ;

        nextCondition = nextPhase != null &&
            //(!nextPhase.needs1Charge || Input.Charge1.IsPressed() || Input.Charge2.IsPressed()) &&
            (!nextPhase.needs2Charge || (Input.Charge1.IsPressed() && Input.Charge2.IsPressed())) &&
            (!nextPhase.needsRagingUpgrade || Upgrades.Active.ragingCharge)
            ;
    }

    protected override void OnEnter(SLS.StateMachineH.State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        //if (Machine.finishedSetup && !playerMovementBody.GroundCheck()) 
        //    Machine.SendSignal("WalkOff", false, true);
        if (attackCollider != null) attackCollider.enabled = true;

    }
    protected override void OnExit(SLS.StateMachineH.State next){if(attackCollider != null) attackCollider.enabled = false;}

    public void LandInto()
    {
        bool groundCollide = Player.MovementBody.GroundCheck(out AnchorPoint collideResult);
        if (!groundCollide && Machine.SendSignal(new("WalkOff", 0, true))) return;
        Player.MovementBody.Land(collideResult);
        State.Enter();
        canRoll = true;
        if (onEntry == EntryAnimAction.Play) Player.Animator.Play(onEnterName);
        if (onEntry == EntryAnimAction.CrossFade) Player.Animator.CrossFade(onEnterName, onEnterTime);
        if (onEntry == EntryAnimAction.Trigger) Player.Animator.SetTrigger(onEnterName);
    }
    public void LandInto(StateAnimator.EntryAnimAction onEntry, string onEnterName, float onEnterTime)
    {
        bool groundCollide = Player.MovementBody.GroundCheck(out AnchorPoint collideResult);
        if (!groundCollide && Machine.SendSignal(new("WalkOff", 0, true))) return;
        Player.MovementBody.Land(collideResult);
        State.Enter();
        canRoll = true;
        if (onEntry == EntryAnimAction.Play) Player.Animator.Play(onEnterName);
        if (onEntry == EntryAnimAction.CrossFade) Player.Animator.CrossFade(onEnterName, onEnterTime);
        if (onEntry == EntryAnimAction.Trigger) Player.Animator.SetTrigger(onEnterName);
    }

        public void StartRoll()
    {
        if (canRoll)
        {
            canRoll = false;
            rollState.State.Enter();
            Machine.SendSignal("EndRoll");
        }
        
    }

    public void EndRoll()
    {
        StartCoroutine(RollCoroutine());
    }
    public IEnumerator RollCoroutine()
    {
        yield return new WaitForSeconds(0.2f);
        walkState.State.Enter();
        //Debug.Log("ending roll attempt!");
        Machine.SendSignal("ResetRoll");
    }

    public void ResetRoll()
    {
        StartCoroutine(ResetRollTimer());
    }

        public IEnumerator ResetRollTimer()
    {
        yield return new WaitForSeconds(2f);
        canRoll = true;
        //Debug.Log("Roll Ready!");
    }





    public StateAnimator.EntryAnimAction onEntry;
    [SerializeField, ShowField(nameof(__showOnEnterName))] public string onEnterName;
    [SerializeField, ShowField(nameof(__showOnEnterTime))] public float onEnterTime;

    #region Edtior
    private bool __showOnEnterName => onEntry != EntryAnimAction.None;
    private bool __showOnEnterTime => onEntry == EntryAnimAction.CrossFade;

    #endregion 
}
