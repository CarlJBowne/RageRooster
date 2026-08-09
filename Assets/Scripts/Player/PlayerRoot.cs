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

/// <summary>
/// The Root component of the Player entity. Implements IPlayer for external access and IPlayerRoot for internal assembly access.
/// </summary>
[DefaultExecutionOrder(ExecutionOrders.Player), RequireComponent(typeof(PlayerStateMachine))]
public class PlayerRoot : MonoBehaviour, IPlayer
{
    #region IPlayer Implementation

    IPlayerStateMachine IPlayer.StateMachine => StateMachine;

    int IPlayer.CurrencyCurrent => currency.Current;
    event Action<int> IPlayer.OnUpdateCurrency 
    { 
        add => currency.updateCurrency += value; 
        remove => currency.updateCurrency -= value; 
    }

    PlayerStats Stats => PlayerStats.Active;

    event Action IPlayer.OnMovingUpdate 
    { 
        add => PlayerMovementBody.MovingUpdateAction += value; 
        remove => PlayerMovementBody.MovingUpdateAction -= value; 
    }
    #endregion

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
    public Action onRespawn;

    public void Awake()
    {
        DontDestroyOnLoad(this);
        
        Self.Instance = this;
        Services.Player = this;

        StateMachine = GetComponent<PlayerStateMachine>();
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
        
        health.Initialize(this);
        ammo.Initialize(this);
        currency.Initialize();

        _activeState = ActivityStates.Active;

        fallDownPitTime = health.playerObject.inFallDownPitTime;
        deathTime = health.playerObject.inDeathTime;
    }

    void OnDestroy()
    {
        if (Self.Instance == (IPlayerRoot)this) Self.Instance = null;
        Services.Register.Player(null);
        _activeState = ActivityStates.Null;
    }
    #endregion

    #region Models (Health / Ammo / Currency)
    public readonly HealthModel health = new();
    public readonly AmmoModel ammo = new();
    public readonly CurrencyModel currency = new();

    public class HealthModel
    {
        private int current;
        private int max;
        public PlayerHealth playerObject;

        public void Initialize(PlayerRoot root)
        {
            playerObject = root.GetComponent<PlayerHealth>();
            max = SaveData.Active.playerStats.maxHealth;
            current = max;
        }

        public int Current
        {
            get => current;
            set
            {
                if (value > max) value = max;
                if (current == value) return;
                current = value;
                updateHealth?.Invoke();
            }
        }
        public int Max
        {
            get => max;
            set
            {
                if (max == value) return;
                max = value;
                SaveData.Active.playerStats.maxHealth = value;
                updateMaxHealth?.Invoke();
            }
        }
        public Action updateHealth;
        public Action updateMaxHealth;
    }

    public class AmmoModel
    {
        private int current;
        private int max;
        public PlayerRanged playerObject;

        public void Initialize(PlayerRoot root)
        {
            playerObject = root.GetComponent<PlayerRanged>();
            max = SaveData.Active.playerStats.maxAmmo;
            current = max;
        }

        public int Current
        {
            get => current;
            set
            {
                if (value > max) value = max;
                if (current == value) return;
                current = value;
                updateAmmo?.Invoke();
            }
        }
        public int Max
        {
            get => max;
            set
            {
                if (max == value) return;
                max = value;
                SaveData.Active.playerStats.maxAmmo = value;
                updateMaxAmmo?.Invoke();
            }
        }
        public Action updateAmmo;
        public Action updateMaxAmmo;
    }

    public class CurrencyModel
    {
        private int current;
        public void Initialize()
        {
            current = SaveData.Active.playerStats.currency;
        }
        public int Current
        {
            get => current;
            set
            {
                if (current == value) return;
                current = value;
                SaveData.Active.playerStats.currency = value;
                updateCurrency?.Invoke();
            }
        }
        public Action<int> updateCurrency;
    }
    #endregion

    #region Death / Respawn Sequence
    private float fallDownPitTime;
    private float deathTime;

    public event Action OnRespawn;

    public void Death()
    {
        DeathOrPit();
        StartCoroutine(DeathRoutine());
        IEnumerator DeathRoutine()
        {
            yield return new WaitForSecondsRealtime(fallDownPitTime + 1);
            yield return OverlayTopPlus.Get.GameOverAnim();
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
}