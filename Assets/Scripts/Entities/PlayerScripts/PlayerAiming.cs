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
    public Transform pointerStart;
    public Transform pointerTarget;
    public Transform spine1;
    public Transform rightWrist;
    public Transform shootMuzzle;
    public float pointerDistance;
    public LayerMask pointerLayerMask;
    public Cinemachine.CinemachineVirtualCamera shootingVCam;
    public State idleState;
    public PlayerRanged grabber;
    public ObjectPool eggPool;

    [HideProperty] public float eggAmount = 10;
    [HideProperty] public int eggCapacity = 10;

    // private Vector3 spine1Offset = new(-0.447f, -0.728f, 6.168f); Let this be a warning to all those who oppose me.

    public override void OnUpdate()
    {
        if(Input.Shoot.WasPressedThisFrame() && M.signalReady) Shoot();

        if (M.signalReady && !Input.ShootMode.IsPressed()) ExitMode();
    }

    private void LateUpdate()
    {
        M.animator.Update(0f);

        spine1.eulerAngles -= Vector3.forward * pointerStart.eulerAngles.x;
        rightWrist.LookAt(pointerTarget);
        shootMuzzle.LookAt(pointerTarget);
    }

    public override void OnFixedUpdate()
    {
        Vector2 mouseInput = Input.Camera;
        body.currentDirection = Vector3.RotateTowards(
            body.currentDirection,
            body.currentDirection.Rotate(mouseInput.x, Vector3.up),
            rotationSpeed.x * Mathf.PI * Time.fixedTime, 0);

        pointerStart.localRotation = Quaternion.RotateTowards(
            pointerStart.localRotation,
            Quaternion.AngleAxis(pointerStart.localEulerAngles.x - mouseInput.y, Vector3.right),
            rotationSpeed.y * Mathf.PI * Time.fixedTime);

        //Note, Clamping is broken, find a way to fix later.

        pointerTarget.position = Physics.Raycast(pointerStart.position, pointerStart.forward, out RaycastHit hit, pointerDistance, pointerLayerMask)
            ? hit.point
            : pointerStart.position + pointerStart.forward * pointerDistance;

        base.OnFixedUpdate();

    }
    public void EnterMode()
    {
        M.animator.CrossFade("GrabAim.GunAim", 0.1f);
        state.TransitionTo();
        shootingVCam.Priority = 11;
    }
    public void ExitMode()
    {
        M.animator.CrossFade("GrabAim.Null", 0.1f);
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

    public void UpdateEggTimer()
    {
        if (eggAmount < eggCapacity)
        {
            eggAmount += Time.deltaTime;
            if (eggAmount > eggCapacity) eggAmount = eggCapacity;
        }
    }

    public void Shoot()
    {
        if(grabber.currentGrabbed != null)
        {

        }
        else if (eggAmount >= 1)
            eggPool.Pump().PlaceAtMuzzle(shootMuzzle);
        
    }
}
