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

    public override bool HorizontalMovement(out float resultX, out float resultZ)
    {
        //Vector3 realDirection = Cameras.RealCamera.transform.TransformDirection(Input.Movement.normalized.ToXZ());
        Vector3 realDirection = Player.Controller.camAdjustedMovement;
        resultX = realDirection.x * walkingSpeed;
        resultZ = realDirection.z * walkingSpeed;
        return true;
    }
}
