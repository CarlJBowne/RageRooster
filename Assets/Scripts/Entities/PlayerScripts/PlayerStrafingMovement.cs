using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using UnityEngine.Windows;
using Cinemachine;
using Cinemachine.Utility;
using static Cinemachine.CinemachineFreeLook;

public class PlayerStrafingMovement : PlayerMovementEffector
{
    public float walkingSpeed;

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float deltaTime = Time.fixedDeltaTime / 0.02f;
        Vector3 controlDirection = Input.Movement.normalized.ToXZ();

        Vector3 realDirection = transform.TransformDirection(controlDirection);
        resultX = realDirection.x * walkingSpeed;
        resultZ = realDirection.z * walkingSpeed;
        playerMovementBody.CurrentSpeed = realDirection.magnitude;
    }
}
