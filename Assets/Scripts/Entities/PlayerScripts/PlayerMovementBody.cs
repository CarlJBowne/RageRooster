using System;
using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using RageRooster.Systems.SaveSystem;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using Utilities.Singletons;
using Utilities.Xtensions.Unity;
using Utilities.Xtensions;
using Cinemachine.Utility;
using RageRooster.Physics;
using RageRooster.RoomSystem;


#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEditor;
using System.Reflection;
#endif


[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(NavMeshAgent))]
public sealed class PlayerMovementBody : PhysicsBody
{
    #region Config

    /// <summary>
    /// Angle threshold for Bonking.
    /// </summary>
    [SerializeField] float bonkThreshold = 15;
    //[SerializeField] float platformDetectionFactor = 3;
    //[SerializeField] float platformLockRadius = .25f;

    internal bool canDoDoubleJump = true;

    #endregion

    #region LifeCycle

    protected override void Awake()
    {
        base.Awake();
        Singleton.Register(this);
    }
    private void Start()
    {
        //Do this again because no matter what I tell this system to do nothing ever works correctly.
        if (Resolvers.defaultGroundedIndex != -1 && Ground.InstantSnapToFloor(out RaycastHit hit))
        {
            Ground.Land(hit);
            Resolvers.Update(Resolvers.defaultGroundedIndex);
        }
        else if (Resolvers.defaultAirIndex != -1)
        {
            Resolvers.Update(Resolvers.defaultAirIndex);
        }
        else enabled = false; //WTF.

    }

    void OnDestroy() => Singleton.Deregister(this);

    static Singleton<PlayerMovementBody> Singleton;
    public static PlayerMovementBody Get => Singleton.Get;
    public static bool TryGet(out PlayerMovementBody result) => Singleton.TryGet(out result);
    public static bool Loaded => Singleton.Active;

    #endregion LifeCycle

    public void ReturnToNeutral(bool doCrossFade = true)
    {
        if (Ground.Check(out _))
        {
            Player.StateMachine.IdleWalk.State.Enter();
            if (doCrossFade) Player.Animator.CrossFade("GroundBasic", .1f);
        }
        else Player.StateMachine.Airborne.Enter();
    }

    protected override void FixedUpdate()
    {
        Player.Animator.SetFloat("CurrentSpeed", Velocity.magnitudeH);
        if (Upgrades.Active.d_moonJump && Input.Jump.IsPressed()) Velocity.u = 10f;

        Vector3 prePos = Position;

        base.FixedUpdate();

        if (prePos != Position) _movingUpdateActionTimer.Tick(MovingUpdateAction);
    }



    public override void OnLand(bool wasntGrounded, bool objectChange)
    {
        Resolvers.Update();
        Player.StateMachine.SendSignal(new("Land", ignoreLock: true));
        canDoDoubleJump = true; //I still don't like this being part of this script of all things.
        if (Player.Controller.CheckJumpBuffer()) Player.StateMachine.SendSignal("Jump");
    }
    public override void OnUnLand(GroundState.Values newValue) => Resolvers.Update();

    public override void WalkOff()
    {
        Ground.UnLand(GroundState.Values.Hangtime);
        Player.StateMachine.SendSignal(new("WalkOff", ignoreLock: true));
    }

    public override bool LastChanceStopper(Vector3 velocity, Vector3 normal)
    {
        if (Vector3.Angle(velocity, -normal) < bonkThreshold && Player.StateMachine.SendSignal(new("Bonk", 0, true)))
        {
            this.Velocity.ZeroOut();
            return true;
        }
        return false;
    }


    #region Other

    public static System.Action MovingUpdateAction;
    private Timer.Loop _movingUpdateActionTimer = new(0.2f);

    public VolcanicVent CurrentVent
    {
        get => currentVent;
        set
        {
            currentVent = value;
            Player.StateMachine.SendSignal(new(value != null ? "EnterVent" : "ExitVent", 0, true));
        }
    }
    VolcanicVent currentVent;
    public bool isOverVent => currentVent != null;

    #endregion Other


#if UNITY_EDITOR

    [CustomEditor(typeof(PlayerMovementBody))]
    new public class Editor : PhysicsBody.Editor
    {
        PropertyField BonkField;

        public override void MakeConfigTab()
        {
            base.MakeConfigTab();
            BonkField = new PropertyField(serializedObject.FindProperty(nameof(PlayerMovementBody.bonkThreshold)));
            ConfigTab.Add(BonkField);
        }
    }
#endif
}