using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAiming : PlayerMovementEffector
{
    public float walkingSpeed;
    public Vector2 rotationSpeed;
    public Vector2 yRotationLimit = new(-90, 45);
    public Transform pointerStart;
    public Transform pointerTarget;
    public float pointerDistance;
    public LayerMask pointerLayerMask;
    public Cinemachine.CinemachineVirtualCamera shootingVCam;
    public State idleState;


    public override void OnFixedUpdate()
    {
        Vector2 mouseInput = Input.Camera;
        body.currentDirection = Vector3.RotateTowards(
            body.currentDirection, 
            body.currentDirection.Rotate(mouseInput.x, Vector3.up), 
            rotationSpeed.x * Mathf.PI * Time.fixedDeltaTime, 0);

        pointerStart.localRotation = Quaternion.RotateTowards(
            pointerStart.localRotation,
            Quaternion.AngleAxis(pointerStart.localEulerAngles.x - mouseInput.y, Vector3.right), 
            rotationSpeed.y * Mathf.PI * Time.fixedDeltaTime);

        //Note, Clamping is broken, find a way to fix later.

        pointerTarget.position = Physics.Raycast(pointerStart.position, pointerStart.forward, out RaycastHit hit, pointerDistance, pointerLayerMask)
            ? hit.point
            : pointerStart.position + pointerStart.forward * pointerDistance;

        base.OnFixedUpdate();

        if (!Input.ShootMode.IsPressed()) ExitMode();
    }

    public void EnterMode()
    {
        state.TransitionTo();
        shootingVCam.Priority = 11;
    }
    public void ExitMode()
    {
        idleState.TransitionTo();
        shootingVCam.Priority = 9;
        pointerStart.localEulerAngles = new(0, 0, 0);
        pointerTarget.position = pointerStart.position + pointerStart.forward * pointerDistance;
    }

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float deltaTime = Time.fixedDeltaTime / 0.02f;
        Vector3 controlDirection = Input.Movement.normalized.ToXZ();

        Vector3 realDirection = transform.TransformDirection(controlDirection);
        resultX = realDirection.x * walkingSpeed;
        resultZ = realDirection.z * walkingSpeed;

    }
}
