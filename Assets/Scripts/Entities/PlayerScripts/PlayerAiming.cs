using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;

public class PlayerAiming : PlayerMovementEffector
{
    public float walkingSpeed;
    public Vector2 rotationSpeed;
    public Vector2 yRotationLimit = new(-90, 45);
    /// <summary>
    /// The Player Ranged Component managing everything.
    /// </summary>
    public PlayerRanged R;


    [HideProperty] public float pointerVRot = 0;
    // private Vector3 spine1Offset = new(-0.447f, -0.728f, 6.168f); Let this be a warning to all those who oppose me.


    public override void OnFixedUpdate()
    {
        Vector2 mouseInput = Input.Camera;

        R.pointerH = Mathf.MoveTowards(R.pointerH,
            R.pointerH + mouseInput.x,
            rotationSpeed.x * Mathf.PI * Time.fixedTime);

        pointerVRot = Mathf.MoveTowards(
            pointerVRot, pointerVRot - mouseInput.y,
            rotationSpeed.y * Mathf.PI * Time.fixedTime
            ).Clamp(yRotationLimit.x, yRotationLimit.y);
        R.pointerV = pointerVRot;

        base.OnFixedUpdate();
    }
    //public void EnterMode()
    //{
    //    M.animator.CrossFade("GrabAim.GunAim", 0.1f);
    //    state.TransitionTo();
    //    shootingVCam.Priority = 11;
    //}
    //public void ExitMode()
    //{
    //    M.animator.CrossFade("GrabAim.Null", 0.1f);
    //    idleState.TransitionTo();
    //    shootingVCam.Priority = 9;
    //    pointerStart.localEulerAngles = new(0, 0, 0);
    //    pointerTarget.position = pointerStart.position + pointerStart.forward * pointerDistance;
    //}

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float deltaTime = Time.fixedDeltaTime / 0.02f;
        Vector3 controlDirection = Input.Movement.normalized.ToXZ();

        Vector3 realDirection = transform.TransformDirection(controlDirection);
        resultX = realDirection.x * walkingSpeed;
        resultZ = realDirection.z * walkingSpeed;
    }

    public void ResetPointerStartRotation()
    {
        pointerVRot = 0;
        R.pointerStartV.localEulerAngles = new Vector3(0, 0, 0);
    }

}
