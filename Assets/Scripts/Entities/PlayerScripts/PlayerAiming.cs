using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using UnityEngine.Windows;
using Cinemachine;
using Cinemachine.Utility;
using static Cinemachine.CinemachineFreeLook;

public class PlayerAiming : PlayerMovementEffector
{
    public float walkingSpeed;
    /// <summary>
    /// The Player Ranged Component managing everything.
    /// </summary>
    public PlayerRanged playerRanged;
    public Cinemachine.CinemachineFreeLook shootFreeLookCamera;

    public Cinemachine.AxisState hAxis;
    public Cinemachine.AxisState vAxis;

    public Vector2 dInput; 

    protected override void OnFixedUpdate()
    {

        dInput = Input.Camera;

        hAxis.m_InputAxisValue = Input.Camera.x;
        hAxis.Update(Time.deltaTime);
        playerRanged.pointerH = hAxis.Value;

        shootFreeLookCamera.m_XAxis.Value = hAxis.Value;

        vAxis.m_InputAxisValue = Input.Camera.y;
        vAxis.Update(Time.deltaTime);
        playerRanged.pointerV = vAxis.Value;

        shootFreeLookCamera.m_YAxis.Value = vAxis.Value.Recast(vAxis.m_MinValue, vAxis.m_MaxValue, 0,1);




        base.OnFixedUpdate(); 
    }


    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float deltaTime = Time.fixedDeltaTime / 0.02f;
        Vector3 controlDirection = Input.Movement.normalized.ToXZ();

        Vector3 realDirection = transform.TransformDirection(controlDirection);
        resultX = realDirection.x * walkingSpeed;
        resultZ = realDirection.z * walkingSpeed;
        playerMovementBody.CurrentSpeed = realDirection.magnitude;
    }

    public void ResetPointerStartRotation()
    {
        playerRanged.pointer.startV.localEulerAngles = new Vector3(0, 0, 0);
    }

}
