using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-99), RequireComponent(typeof(PlayerStateMachine))]
public class Player : MonoBehaviour
{
    public static bool Exists { get; private set; } = false;
    public static bool Active { get; private set; } = false;

    public static GameObject GameObject { get; private set; }
    public static Transform Transform { get; private set; }
    public static PlayerStateMachine StateMachine { get; private set; }
    public static PlayerMovementBody MovementBody { get; private set; }
    public static CapsuleCollider Collider { get; private set; }
    public static PlayerController Controller { get; private set; }
    public static PlayerRanged Ranged { get; private set; }
    public static PlayerInteracter Interacter { get; private set; }
    public static PlayerHealth Health { get; private set; }
    public static Animator Animator { get; private set; }
    public static AudioCaller Audio { get; private set; }

    public static Vector3 Position => Transform.position;
    public static Quaternion Rotation => Transform.rotation;
    public static Vector3 Forward => Transform.forward;
    public static Vector3 EularAngles => Transform.eulerAngles;

    #region Instance Fields







    #endregion Instance Fields

    public void Awake()
    {
        GameObject = gameObject;
        Transform = transform;
        StateMachine = GetComponent<PlayerStateMachine>();
        MovementBody = GetComponent<PlayerMovementBody>();
        Collider = GetComponent<CapsuleCollider>();
        Controller = GetComponent<PlayerController>();
        Ranged = GetComponent<PlayerRanged>();
        Interacter = GetComponent<PlayerInteracter>();
        Health = GetComponent<PlayerHealth>();
        Animator = GetComponent<Animator>();
        Audio = GetComponent<AudioCaller>();

        Exists = true;
        Active = true;
    }

    public static void SetActive(bool active)
    {
        if (!Exists) return;
        Active = active;
        GameObject.SetActive(active);
        //StateMachine.enabled = active;
        //MovementBody.enabled = active;
        //Controller.enabled = active;
        //Ranged.enabled = active;
        //Interacter.enabled = active;
        //Health.enabled = active;
        //Collider.enabled = active;
        //Animator.enabled = active;
        //Audio.enabled = active;
    }

    public static void InstantMove(Vector3 newPosition, float? yRot = null)
    {
        if (!Exists) return;
        Vector3 camDelta = newPosition - Transform.position;
        MovementBody.ForceSetPosition(newPosition);
        if (yRot != null) MovementBody.Rotation = new(0, yRot.Value, 0);
        StateMachine.ResetState();
        Cameras.currentVirtualCamera.PreviousStateIsValid = false;
        Cameras.currentVirtualCamera.OnTargetObjectWarped(Transform, camDelta);
        MovementBody.velocity = Vector3.zero;
    }
    public static void InstantMove(SavePoint_Old savePoint)
    {
        if (!Exists) return;
        Vector3 camDelta = savePoint.SpawnPoint.position - Transform.position;
        MovementBody.ForceSetPosition(savePoint.SpawnPoint.position);
        MovementBody.Rotation = new(0, savePoint.SpawnPoint.eulerAngles.y, 0);
        StateMachine.ResetState();
        Ranged.Release(Vector3.zero, false);
        Cameras.currentVirtualCamera.PreviousStateIsValid = false;
        Cameras.currentVirtualCamera.OnTargetObjectWarped(Transform, camDelta);
        MovementBody.velocity = Vector3.zero;
        MovementBody.InstantSnapToFloor();
        savePoint.onSpawnEvent?.Invoke();
    }




}
