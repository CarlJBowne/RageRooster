using EditorAttributes;
using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Utilities.Xtensions.Unity;

[System.Obsolete("PlayerMovementAnimator is deprecated, please use TimedMovementAffector instead.")]
public class PlayerMovementAnimator : PlayerMovementEffector
{
    [Tooltip("Generally recommended to keep at 0 and have set to 1 in animation so that the CrossFade can automatically smoothly blend the effect."), Range(0, 1)]
    public float influence;
    public bool fullStop;

    public float maxSpeed = 0;
    public float minSpeed = 0;
    public float speedChangeRate = 15;
    public float turnability = 10;
    public float verticalAddSpeed;
    public float terminalVelocity = 98.1f;

    [Tooltip("Sets/Lerps the velocity to a specific point rather than adding it.")]
    [Range(0, 1)] public float setVerticalInfluence;
    public float setVerticalVelocity;
    [Tooltip("Only active if locked.")]
    public float defaultGravity;

    [Range(0, 1)] public float worldspaceInfluence;
    public Vector3 worldspaceVelocity;

    [Tooltip("Makes this Movement Effector inoperable no matter the parameters. Must be set by some kind of alternative source, or by an inheriting class.")]
    public bool locked;

    public override bool ForwardMovement(out float result)
    {
        if (locked)
        {
            base.ForwardMovement(out result);
            return true;
        }
        if (fullStop)
        {
            result = 0;
            return true;
        }
        

        result = Player.MovementBody.Velocity.f;

        if (influence > 0)
        {
            Vector3 controlVector = Player.Controller.camAdjustedMovement;

            Vector3 targetDirection = Player.MovementBody.Direction;
            float targetSpeed = Player.MovementBody.Velocity.f;

            if (turnability > 0) targetDirection = Vector3.RotateTowards(targetDirection, controlVector.normalized, turnability * Mathf.PI * Time.fixedDeltaTime, 0);

            targetSpeed = controlVector.sqrMagnitude > 0
                ? targetSpeed.Move(controlVector.magnitude * speedChangeRate * (Time.deltaTime * 50), maxSpeed)
                : targetSpeed.Move(speedChangeRate * (Time.deltaTime * 50), minSpeed);

            if (influence == 1)
            {
                Player.MovementBody.Velocity.f = targetSpeed;
                Player.MovementBody.Direction.Set(targetDirection);
            }
            else
            {
                Player.MovementBody.Velocity.f = Mathf.Lerp(Player.MovementBody.Velocity.f, targetSpeed, influence);
                Player.MovementBody.Direction.Set(Vector3.Lerp(Player.MovementBody.Direction, targetDirection, influence));
            }

        }
        if (worldspaceInfluence > 0)
        {
            Vector3 relative = transform.TransformDirection(worldspaceVelocity);
            result = worldspaceInfluence == 1
                ? relative.x
                : Mathf.Lerp(result, relative.x, worldspaceInfluence);
            result = worldspaceInfluence == 1
                ? relative.z
                : Mathf.Lerp(result, relative.z, worldspaceInfluence);
        }
        return true;
    }
    public override bool VerticalMovement(out float result)
    {
        if (locked)
        {
            result = Player.MovementBody.Velocity.y - defaultGravity * .02f;
            return true;
        }

        result = Player.MovementBody.Velocity.y;

        if (influence > 0 && !Mathf.Approximately(verticalAddSpeed, 0)) result = (result + verticalAddSpeed * Time.fixedDeltaTime * influence).Min(-terminalVelocity);
        if (setVerticalInfluence > 0)
            result = setVerticalInfluence == 1
                ? setVerticalVelocity
                : Mathf.Lerp(result, setVerticalVelocity, setVerticalInfluence);
        if (worldspaceInfluence > 0)
        {
            result = worldspaceInfluence == 1
                ? worldspaceVelocity.y
                : Mathf.Lerp(result, worldspaceVelocity.y, worldspaceInfluence);
        }
        if (fullStop)
        {
            result = 0;
        }
        return true;
    }

    protected override void OnExit(State next)
    {
        locked = false;
    }




    public string intendedAnimationName;

    [ContextMenu("Recast")]
    public virtual void RunTransfer() => MiscHelperMethods.PlayerMovementAnimatorTransferToRoots.Basic(this);


    public void ResetActive()
    {
        influence = 0;
        fullStop = false;
    }


}
