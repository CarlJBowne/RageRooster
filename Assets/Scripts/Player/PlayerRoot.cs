using System;
using System.Collections;
using System.Collections.Generic;
using RageRooster;
using RageRooster.Core;
using RageRooster.Core.Save;
using RageRooster.Player;
using RageRooster.World;
using SLS.MenuCore;
using SLS.StateMachineH.Signals;
using UnityEngine;
using UnityEngine.AI;
using static SLS.Singletons.Singleton;
using static RageRooster.Services;
using static RageRooster.Player.Services;
using SLS.GeneralUtilities.EventTickets;

/// <summary>
/// The Root component of the Player entity. Implements IPlayer for external access and IPlayerRoot for internal assembly access.
/// </summary>
[DefaultExecutionOrder(ExecutionOrders.Player), RequireComponent(typeof(PlayerStateMachine))]
public class PlayerRoot : MonoBehaviour, IPlayer
{

    #region GameplayState

    public ActivityStates ActivityState
    {
        get => _activeState;
        set
        {
            if (_activeState == value || value is ActivityStates.Null || _activeState is ActivityStates.Null)
                return;

            _activeState = value;

            gameObject.SetActive(value != ActivityStates.Invisible);
            StateMachine.enabled = value is ActivityStates.Active;

            MovementBody.enabled = value is ActivityStates.Active or ActivityStates.Dying;
            Controller.enabled = value is ActivityStates.Active;
            Animator.enabled = value is ActivityStates.Active or ActivityStates.Cutscene;
            Ranged.enabled = value is ActivityStates.Active;
        }
    }

    private ActivityStates _activeState = ActivityStates.Null;

    public bool Exists => ActivityState is not ActivityStates.Null;
    public bool Active => ActivityState is ActivityStates.Active;
    public bool Paused => ActivityState is ActivityStates.Paused;
    public bool InCutscene => ActivityState is ActivityStates.Cutscene;
    public bool Dying => ActivityState is ActivityStates.Dying;

    #endregion

    #region Component References
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;
    public PlayerStateMachine StateMachine { get; private set; }
    public PlayerHealth Health { get; private set; }
    public SignalManager SignalManager { get; private set; }
    public PlayerMovementBody MovementBody { get; private set; }
    public CapsuleCollider Collider { get; private set; }
    public PlayerController Controller { get; private set; }
    public PlayerRanged Ranged { get; private set; }
    public PlayerGrabber Grabber { get; private set; }
    public TargetingManager TargetingManager { get; private set; }
    public Animator Animator { get; private set; }
    public AudioCaller Audio { get; private set; }
    public RagdollHandler RagdollHandler { get; private set; }
    public PlayerStats Stats => PlayerStats.Active;
    #endregion

    #region Helper Properties / Methods
    public Vector3 Position => Exists ? transform.position : Vector3.zero;
    public Vector3 Center => transform.position + Collider.center;
    public Quaternion Rotation => transform.rotation;
    public Vector3 Forward => transform.forward;

    public MonoBehaviour CurrentVent { get; set; }

    public float DistanceFrom(Vector3 pos) => Exists ? Vector3.Distance(Position, pos) : 999999f;

    public void InstantMove(Vector3 newPosition, float? yRot = null)
    {
        if (!Exists) return;
        Vector3 camDelta = newPosition - transform.position;
        MovementBody.Position = newPosition;
        if (yRot != null) MovementBody.Direction.Rotation = new(0, yRot.Value, 0);
        StateMachine.ResetState();
        Cameras.currentVirtualCamera.PreviousStateIsValid = false;
        Cameras.currentVirtualCamera.OnTargetObjectWarped(transform, camDelta);
        MovementBody.Velocity.ZeroOut();
    }

    public bool Owns(Component C) => Exists && C != null && C.gameObject == gameObject;

    #endregion

    #region Events / Callbacks

    private List<EventTicket> events;
    public void Awake()
    {
        DontDestroyOnLoad(this);

        RageRooster.Player.Services.Player = this;

        StateMachine = GetComponent<PlayerStateMachine>();
        Health = GetComponent<PlayerHealth>();
        MovementBody = GetComponent<PlayerMovementBody>();
        Collider = GetComponent<CapsuleCollider>();
        Controller = GetComponent<PlayerController>();
        Ranged = GetComponent<PlayerRanged>();
        Grabber = GetComponent<PlayerGrabber>();
        Animator = GetComponent<Animator>();
        Audio = GetComponent<AudioCaller>();
        RagdollHandler = GetComponent<RagdollHandler>();
        TargetingManager = GetComponent<TargetingManager>();
        SignalManager = GetComponent<SLS.StateMachineH.Signals.SignalManager>();

        _activeState = ActivityStates.Active;

        fallDownPitTime = Health.inFallDownPitTime;
        deathTime = Health.inDeathTime;
    }
    private void Start()
    {
        events = new()
        {
            Health.OnValueChanged.Subscribe(UIHUDSystem.Instance.health.Update),
            Health.OnMaxChanged.Subscribe(UIHUDSystem.Instance.health.UpdateMax),
            Ranged.Ammo.Subscribe(UIHUDSystem.Instance.health.Update),
            Ranged.Ammo.SubscribeMax(UIHUDSystem.Instance.ammo.UpdateMax),
            SavedProgress.Active.Currency.Subscribe(UIHUDSystem.Instance.SetCurrencyText),
        };
    }

    void OnDestroy()
    {
        RageRooster.Services.Player = null;
        _activeState = ActivityStates.Null;
        events.DestroyAll();
    }
    #endregion


    #region Death / Respawn Sequence
    private float fallDownPitTime;
    private float deathTime;

    public Action onRespawn { get; set; }
    public event Action OnRespawn
    {
        add => onRespawn += value;
        remove => onRespawn -= value;
    }
    public Action onMovingUpdate { get; set; }
    public event Action OnMovingUpdate
    {
        add => onMovingUpdate += value;
        remove => onMovingUpdate -= value;
    }

    public void Death()
    {
        DeathOrPit();
        StartCoroutine(DeathRoutine());
        IEnumerator DeathRoutine()
        {
            yield return new WaitForSecondsRealtime(fallDownPitTime + 1);
            yield return UI.OverlayTopPlus.GameOverAnimation();
            yield return new WaitForSecondsRealtime(deathTime);

            RoomManager.TransitionStyle = new()
            {
                FadeOutRoutine = Overlay.UnderHUD.FadeAlpha(1, 1f),
                FadeInRoutine = Overlay.UnderHUD.FadeAlpha(0, 1f),
            };

            Gameplay.Death();
        }
    }

    public void PitFall()
    {
        DeathOrPit();
        StartCoroutine(PitFallRoutine());
        IEnumerator PitFallRoutine()
        {
            yield return new WaitForSecondsRealtime(fallDownPitTime);

            RoomManager.TransitionStyle = new()
            {
                FadeOutRoutine = Overlay.UnderHUD.FadeAlpha(1, 1f),
                FadeInRoutine = Overlay.UnderHUD.FadeAlpha(0, 1f),
            };

            Gameplay.Respawn();
        }
    }

    private void DeathOrPit()
    {
        Vector3 targetVelocity = MovementBody.Velocity.Global;
        Audio.PlayOneShot("Death");
        StateMachine.Ragdoll.Enter();
        MovementBody.Velocity.ZeroOut();
        RagdollHandler.State = RagdollHandler.States.Thrown;
        RagdollHandler.SetVelocity(targetVelocity * 0.75f);
        Animator.enabled = false;
    }
    #endregion

    IPlayerStateMachine IPlayer.StateMachine => StateMachine;
}