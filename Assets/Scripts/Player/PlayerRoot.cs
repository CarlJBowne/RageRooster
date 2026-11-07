using System;
using System.Collections;
using System.Collections.Generic;
using log4net;
using RageRooster;
using RageRooster.Core;
using RageRooster.Core.Save;
using RageRooster.Core.World;
using RageRooster.Player;
using SLS.GeneralUtilities.Events;
using SLS.MenuCore;
using SLS.StateMachineH.Signals;
using UnityEngine;
using UnityEngine.AI;
using static RageRooster.Core.IPlayer;

using Core = RageRooster.Services;

namespace RageRooster.Player
{
    /// <summary>
    /// The Root component of the Player entity. Implements IPlayer for external access from the Core assembly.
    /// Handles MonoBehavior lifecycle and initialization of the static Player context.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrders.Player)]
    [RequireComponent(typeof(PlayerStateMachine), typeof(PlayerHealth), typeof(PlayerMovementBody))]
    [RequireComponent(typeof(PlayerGrabber), typeof(PlayerController), typeof(TargetingManager))]
    [RequireComponent(typeof(SignalManager), typeof(Animator), typeof(RagdollHandler))]
    [RequireComponent(typeof(AudioCaller), typeof(CapsuleCollider))]
    public class PlayerRoot : MonoBehaviour, IPlayer
    {
        #region Forwarding Properties to Static Player Context
        public ActivityStates ActivityState
        {
            get => Player.ActivityState;
            set => Player.ActivityState = value;
        }

        public bool Exists => Player.Exists;
        public bool Active => Player.Active;
        public bool Paused => Player.Paused;
        public bool InCutscene => Player.InCutscene;
        public bool Dying => Player.Dying;

        public Transform Transform => Player.Transform;
        public GameObject GameObject => Player.GameObject;
        public PlayerStateMachine StateMachine => Player.StateMachine;
        public PlayerHealth Health => Player.Health;
        public SignalManager SignalManager => Player.SignalManager;
        public PlayerMovementBody MovementBody => Player.MovementBody;
        public CapsuleCollider Collider => Player.Collider;
        public PlayerController Controller => Player.Controller;
        public PlayerRanged Ranged => Player.Ranged;
        public PlayerGrabber Grabber => Player.Grabber;
        public TargetingManager TargetingManager => Player.TargetingManager;
        public Animator Animator => Player.Animator;
        public AudioCaller Audio => Player.Audio;
        public RagdollHandler RagdollHandler => Player.RagdollHandler;
        public PlayerStats Stats => Player.Stats;

        public Vector3 Position => Player.Position;
        public Vector3 Center => Player.Center;
        public Quaternion Rotation => Player.Rotation;
        public Vector3 Forward => Player.Forward;

        public MonoBehaviour CurrentVent
        {
            get => Player.CurrentVent;
            set => Player.CurrentVent = value;
        }

        public float DistanceFrom(Vector3 pos) => Player.DistanceFrom(pos);
        public void InstantMove(Vector3 newPosition, float? yRot = null) => Player.InstantMove(newPosition, yRot);
        public bool Owns(Component C) => Player.Owns(C);
        #endregion

        #region Events / Callbacks

        private List<EventTicket> events;
        public void Awake()
        {
            if (Player.Exists && Player.Root != this)
            {
                Destroy(gameObject);
                return;
            }

            //DontDestroyOnLoad(this);

            RageRooster.Core.IPlayer.Register.Register(this, IPlayerPassword);

            Player.Initialize(this);
        }
        private void Start()
        {
            events = new()
        {
#if UNITY_EDITOR
            Input.Debug.GodMode.SubscribePerformed(PlayerStats.ActivateDebug)
#endif
        };
        }

        void OnDestroy()
        {
            RageRooster.Core.IPlayer.Register.Deregister(this, IPlayerPassword);
            Player.Clear();
            events.DestroyAll();
        }
        #endregion


        #region Death / Respawn Sequence

        public event Action OnRespawn
        {
            add => Player.onRespawn += value;
            remove => Player.onRespawn -= value;
        }
        public event Action OnMovingUpdate
        {
            add => Player.onMovingUpdate += value;
            remove => Player.onMovingUpdate -= value;
        }



        #endregion

        #region Interface Implementation
        IPlayerStateMachine IPlayer.StateMachine => StateMachine;

        public void Death() => Player.Death();

        public void Respawn() => Player.Respawn();

        public static void HaveDestroyed()
        {

        }

        private const string IPlayerPassword = "ANGUS_BUCKEYE_IS_RAD";
        #endregion
    }
}