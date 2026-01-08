using EditorAttributes;
using RageRooster.Systems.SaveSystem;
using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static SLS.StateMachineH.StateAnimator;
using static UnityEngine.EventSystems.EventTrigger;

public class PlayerGroundMovement : PlayerMovementEffector
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

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float currentSpeed = playerMovementBody.CurrentSpeed;
        Vector3 currentDirection = playerMovementBody.direction;

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


        if (controlMag > 0)
        {
            float Dot = Vector3.Dot(controlDirection, currentDirection);

            if (maxTurnSpeed > 0) playerMovementBody.DirectionSet(maxTurnSpeed);

            if (!outwardTurn) currentSpeed *= Dot;


            if (currentSpeed < maxSpeed) currentSpeed = currentSpeed.MoveUp(controlMag * acceleration * deltaTime, maxSpeed);
            else if (currentSpeed > maxSpeed) currentSpeed = currentSpeed.MoveDown(decceleration * deltaTime, maxSpeed);
        }
        else if (minSpeed > 0)
        {
            if (currentSpeed < minSpeed) currentSpeed = currentSpeed.MoveUp(stopping * deltaTime, minSpeed);
            else if (currentSpeed > minSpeed) currentSpeed = currentSpeed.MoveDown(decceleration * deltaTime, minSpeed);
        }
        else currentSpeed = currentSpeed > .01f ? currentSpeed.MoveTowards(currentSpeed * stopping * deltaTime, 0) : 0;
    }
    
    [SerializeField] public string animationName;

    private bool DoEnter()
    {
        bool groundCollide = playerMovementBody.GroundCheck(out AnchorPoint collideResult);
        if (!groundCollide && Machine.SendSignal(new("WalkOff", 0, true))) return true;
        playerMovementBody.Land(collideResult);
        State.Enter();
        return false;
    }
    public void EnterFade()
    {
        if (DoEnter()) return;
        Player.Animator.CrossFade(animationName, .1f);
    }
    public void EnterFade(float time)
    {
        if (DoEnter()) return;
        Player.Animator.CrossFade(animationName, time);
    }
    public void EnterTrigger()
    {
        if (DoEnter()) return;
        Player.Animator.SetTrigger("Land");
    }
    public void EnterTrigger(string triggerName)
    {
        if (DoEnter()) return;
        Player.Animator.SetTrigger(triggerName);
    }
    public void EnterPlay()
    {
        if (DoEnter()) return;
        Player.Animator.Play(animationName);
    }
    public void EnterNoAnimation() => DoEnter();
}
