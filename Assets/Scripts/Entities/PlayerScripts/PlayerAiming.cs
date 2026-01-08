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
    public Transform aimRotationController;
    public Transform targetPos;
    public LayerMask hitScanLayerMask;
    public float playerRotationSpeed = 10f;

    public Cinemachine.AxisState hAxis;
    public Cinemachine.AxisState vAxis;

    [SerializeField] private Vector2 dInput;
    private Vector3 lastAimPos;

    protected override void OnFixedUpdate()
    {
        aimRotationController.position = Player.MovementBody.Position + (Vector3.up * 0.915f);

        dInput = Input.Camera;

        hAxis.m_InputAxisValue = Input.Camera.x;
        hAxis.Update(Time.deltaTime);
        vAxis.m_InputAxisValue = Input.Camera.y;
        vAxis.Update(Time.deltaTime);
        aimRotationController.localEulerAngles = new Vector3(vAxis.Value, hAxis.Value, 0);

        Player.MovementBody.InstantDirectionChange(
            Vector3.RotateTowards(
                Player.MovementBody.direction, aimRotationController.forward.XZ(),
                playerRotationSpeed * Mathf.PI * Time.fixedTime, 0)
            );

        targetPos.position = TargetingManager.RangedChannel.CurrentTarget != null
            ? TargetingManager.RangedChannel.CurrentTarget.position
            : Physics.Raycast(Cameras.aimingCamera.transform.position, Cameras.aimingCamera.transform.forward, out RaycastHit hit, TargetingManager.RangedChannel.Range.maxDistance, hitScanLayerMask)
                ? hit.point
                : Cameras.aimingCamera.transform.position + (Cameras.RealCamera.transform.forward * TargetingManager.RangedChannel.Range.maxDistance);

        this.HorizontalMovement(out float? X, out float? Z);
        this.VerticalMovement(out float? Y);
        playerMovementBody.VelocitySet(X, Y, Z);
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

    public void SetXYRotation(float X, float Y)
    {
        aimRotationController.localEulerAngles = new Vector3(Y, X, 0);
        hAxis.Value = X;
        vAxis.Value = Y;

        //Cameras.aimingCamera.PreviousStateIsValid = false;
        Cameras.aimingCamera.CancelDamping();
        //Cameras.aimingCamera.ForceCameraPosition(Cameras.RealCamera.transform.position, Cameras.RealCamera.transform.rotation);
        //Cameras.aimingCamera.OnTargetObjectWarped(aimRotationController, aimRotationController.position - lastAimPos); 
    }

    public void StoreLastAim() => lastAimPos = aimRotationController.position;

    private static Vector3 defaultDamping = new(.1f, .5f, .3f);

}
