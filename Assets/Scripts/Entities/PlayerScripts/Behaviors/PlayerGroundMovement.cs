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

    public override bool ForwardMovement(out float result)
    {
        float currentSpeed = Player.MovementBody.velocity.f;
        Vector3 currentDirection = Player.MovementBody.DirectionGet;

        float deltaTime = Time.deltaTime * 50;
        Vector3 controlDirection = Player.Controller.camAdjustedMovement.normalized;
        float controlMag = Player.Controller.camAdjustedMovement.magnitude;


        if (controlMag > 0)
        {
            float Dot = Vector3.Dot(controlDirection, currentDirection);

            if (maxTurnSpeed > 0) Player.MovementBody.DirectionSet(maxTurnSpeed * Time.fixedDeltaTime);

            if (!outwardTurn) currentSpeed *= Dot;


            if (currentSpeed < maxSpeed) currentSpeed = currentSpeed.MoveUp(controlMag * acceleration * deltaTime, maxSpeed);
            else if (currentSpeed > maxSpeed) currentSpeed = currentSpeed.MoveDown(decceleration * deltaTime, maxSpeed);
        }
        else if (minSpeed > 0)
        {
            if (currentSpeed < minSpeed) currentSpeed = currentSpeed.MoveUp(stopping * deltaTime, minSpeed);
            else if (currentSpeed > minSpeed) currentSpeed = currentSpeed.MoveDown(decceleration * deltaTime, minSpeed);
        }
        else currentSpeed = currentSpeed > .01f ? currentSpeed.Move(currentSpeed * stopping * deltaTime, 0) : 0;


        Player.MovementBody.velocity.f = currentSpeed;

        result = currentSpeed;
        return true;
    }

    [SerializeField] public string animationName;

    private bool DoEnter()
    {
        //bool groundCollide = playerMovementBody.GroundCheck(out AnchorPoint collideResult, true);
        //if (!groundCollide && Machine.SendSignal(new("WalkOff", 0, true))) return true;
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
    public void EnterFadeSynced(float time) => Player.Animator.CrossFade(name, time, 0, Player.Animator.GetCurrentAnimatorStateInfo(-1).normalizedTime);
}

public static class _________HELPER_PUT_SOMEWHERE_ELSE_LATER
{
    public static void Trigger(this Animator anim, string triggerName)
    {
        Wait().Begin(anim);
        IEnumerator Wait()
        {
            anim.SetTrigger(triggerName);
            yield return null;
            anim.ResetTrigger(triggerName);
        }
    }
}