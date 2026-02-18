using RageRooster.RoomSystem;
using RageRooster.Systems.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A global Singleton representing the Player entity in the game. Provides static access to commonly used components and systems related to the player.
/// </summary>
[DefaultExecutionOrder(ExecutionOrders.Player), RequireComponent(typeof(PlayerStateMachine))]
public class Player : MonoBehaviour
{
    #region GameplayState

    /// <summary>
    /// An enum representing states of activity for the Player. 
    /// </summary>
    public enum ActivityStates
    {
        /// <summary> The <see cref="Player"/> has not been loaded in as <see cref="Gameplay"/> is not active. </summary>
        Null = -1,
        /// <summary> The <see cref="Player"/> is active and controlled by the player. </summary>
        Active = 0,
        /// <summary> The <see cref="Player"/> is paused in place, still visible, but not moving. </summary>
        Paused = 1,
        /// <summary> The player is in the dying animation. </summary>
        Dying = 2,
        /// <summary> The player is outside of the visibly active scene and thus unrendered.</summary>
        Invisible = 3,
        /// <summary> The game is in a cutscene state and all active logic on the <see cref="Player"/> has been paused. </summary>
        Cutscene = 4,
        /// <summary> 
        /// The game is currently in a Minigame state where the player's default behavior is not present. 
        /// <br/> Minigames where the player moves and acts as normal may be implemented in a different way.
        /// </summary>
        Minigame = 5,
    }

    /// <summary>
    /// The current <see cref="ActivityStates"/> of the <see cref="Player"/>.
    /// <br/> Read helpers <see cref="Exists"/>, <see cref="Active"/>, <see cref="Paused"/>, and <see cref="InCutscene"/> are provided for convenience.
    /// </summary>
    public static ActivityStates ActivityState
    {
        get => _activeState;
        set
        {
            // Ignore redundant assignments and any outside attempts to set or come from Null.
            if (_activeState == value
                || value is ActivityStates.Null
                || _activeState is ActivityStates.Null)
                return;

            _activeState = value;

            Visible = value != ActivityStates.Invisible;
            StateMachine.enabled = value is ActivityStates.Active;

            MovementBody.RBState =
                value is ActivityStates.Active ? CharacterMovementBody.BodyState.Enabled
                : value is ActivityStates.Dying ? CharacterMovementBody.BodyState.Ragdoll
                : CharacterMovementBody.BodyState.OFF;

            MovementBody.enabled = value is ActivityStates.Active or ActivityStates.Dying;
            Controller.enabled = value is ActivityStates.Active;
            Animator.enabled = value is ActivityStates.Active or ActivityStates.Cutscene;
            Ranged.enabled = value is ActivityStates.Active;
            //Interacter.enabled = value is ActivityStates.Active;
        }
    }

    private static ActivityStates _activeState = ActivityStates.Null;


    /// <summary>
    /// Whether the <see cref="Player"/> entity has been loaded into the world. 
    /// <br/> Reads <see cref="ActivityState"/>, is true if the <see cref="ActivityState"/> is anything other than <see cref="ActivityStates.Null"/>.
    /// </summary>
    public static bool Exists => ActivityState is not ActivityStates.Null;
    /// <summary>
    /// Whether the <see cref="Player"/> entity is currently active and able to interact with the game world. 
    /// <br/> Reads <see cref="ActivityState"/>, is true if the <see cref="ActivityState"/> is <see cref="ActivityStates.Active"/>.
    /// </summary>
    public static bool Active => ActivityState is ActivityStates.Active;
    /// <summary>
    /// Whether the <see cref="Player"/> entity is currently paused.
    /// <br/> Reads <see cref="ActivityState"/>, is true if the <see cref="ActivityState"/> is <see cref="ActivityStates.Paused"/>.
    /// <br/> Note: Not actually specific to the Pause Menu. Also used during room transitions and other non-interactive states.
    /// </summary>
    public static bool Paused => ActivityState is ActivityStates.Paused;
    /// <summary>
    /// Whether the <see cref="Player"/> entity is currently in a cutscene.
    /// <br/> Reads <see cref="ActivityState"/>, is true if the <see cref="ActivityState"/> is <see cref="ActivityStates.Cutscene"/>.
    /// </summary>
    public static bool InCutscene => ActivityState is ActivityStates.Cutscene;
    /// <summary>
    /// Whether the <see cref="Player"/> entity is currently in the dying ragdoll animation.
    /// <br/> Reads <see cref="ActivityState"/>, is true if the <see cref="ActivityState"/> is <see cref="ActivityStates.Dying"/>.
    /// </summary>
    public static bool Dying => ActivityState is ActivityStates.Dying;
    /// <summary>
    /// Whether the <see cref="Player"/> is currently visible in the game world.
    /// <br/> Reads <see cref="ActivityState"/>, is false if the <see cref="ActivityState"/> is <see cref="ActivityStates.Invisible"/>.
    /// <br/> Setter also privately accessible.
    /// </summary>
    public static bool Visible
    {
        get => Exists && ActivityState is not ActivityStates.Invisible;
        protected set => GameObject.SetActive(value);
    }


    #endregion

    #region Component References
    /// <summary>
    /// The Root <see cref="UnityEngine.GameObject"/> of the <see cref="Player"/>.
    /// </summary>
    public static GameObject GameObject { get; private set; }
    /// <summary>
    /// The Root <see cref="UnityEngine.Transform"/> of the <see cref="Player"/>.
    /// </summary>
    public static Transform Transform { get; private set; }
    /// <summary>
    /// The <see cref="PlayerStateMachine"/> component attached to the <see cref="Player"/>. <br/>
    /// Handles the player's state transitions and logic.
    /// </summary>
    public static PlayerStateMachine StateMachine { get; private set; }

    public static SLS.StateMachineH.Signals.SignalManager SignalManager { get; private set; }
    /// <summary>
    /// The <see cref="PlayerMovementBody"/> component attached to the <see cref="Player"/>. <br/>
    /// Handles movement and physics interactions.
    /// </summary>
    public static PlayerMovementBody MovementBody { get; private set; }
    /// <summary>
    /// The <see cref="CapsuleCollider"/> component attached to the <see cref="Player"/>. <br/>
    /// </summary>
    public static CapsuleCollider Collider { get; private set; }
    /// <summary>
    /// The <see cref="PlayerController"/> component attached to the <see cref="Player"/>. <br/>
    /// Handles player input and control.
    /// </summary>
    public static PlayerController Controller { get; private set; }
    /// <summary>
    /// The <see cref="PlayerRanged"/> component attached to the <see cref="Player"/>. <br/>
    /// Handles ranged combat mechanics.
    /// </summary>
    public static PlayerRanged Ranged { get; private set; }
    /// <summary>
    /// The <see cref="PlayerGrabber"/> component attached to the <see cref="Player"/>.
    /// Handles the grabbing of enemies and objects.
    /// </summary>
    public static PlayerGrabber Grabber { get; private set; }
    /// <summary>
    /// The <see cref="TargetingManager"/> component attached to the <see cref="Player"/>. <br/>
    /// </summary>
    public static TargetingManager TargetingManager;
    /// <summary>
    /// The <see cref="PlayerInteracter"/> component attached to the <see cref="Player"/>. <br/>
    /// Handles interact functionality.
    /// </summary>
    /// <summary>
    /// The <see cref="Animator"/> component attached to the <see cref="Player"/>.
    /// </summary>
    public static Animator Animator { get; private set; }
    /// <summary>
    /// The <see cref="AudioCaller"/> component attached to the <see cref="Player"/>. <br/>
    /// Handles One-Time Sound emission from the <see cref="Player"/>.
    /// </summary>
    public static AudioCaller Audio { get; private set; }
    /// <summary>
    /// The <see cref="RagdollHandler"/> component attached to the <see cref="Player"/>. <br/>
    /// </summary>
    public static RagdollHandler RagdollHandler { get; private set; }
    #endregion

    #region Helper Properties / Methods
    /// <summary>
    /// The current world position of the <see cref="Player"/>. (At Feet.)
    /// </summary>
    public static Vector3 Position => Exists ? Transform.position : Vector3.zero;
    /// <summary>
    /// The current world position of the center of the <see cref="Player"/>'s <br/>
    /// See <see cref="Collider"/>.
    /// </summary>
    public static Vector3 Center => Transform.position + Collider.center;
    /// <summary>
    /// The current Rotation of the <see cref="Player"/> as a Quaternion.
    /// </summary>
    public static Quaternion Rotation => Transform.rotation;
    /// <summary>
    /// The current Forward Vector of the <see cref="Player"/>.
    /// </summary>
    public static Vector3 Forward => Transform.forward;
    /// <summary>
    /// The current Rotation of the <see cref="Player"/> in Euler Angles.
    /// </summary>
    public static Vector3 EularAngles => Transform.eulerAngles;

    /// <summary>
    /// The current velocity of the <see cref="Player"/>'s <see cref="PlayerMovementBody"/>.
    /// </summary>
    public static Vector3 Velocity => MovementBody.velocity;

    /// <param name="pos">The position to be compared.</param>
    /// <returns>The distance between the <see cref="Player"/> and and a given position, such as an enemy.</returns>
    public static float DistanceFrom(Vector3 pos) => Exists ? Vector3.Distance(Position, pos) : 999999f;

    /// <summary>
    /// Instantly moves the <see cref="Player"/> to a new position, optionally setting a new Y rotation. <br/>
    /// </summary>
    /// <param name="newPosition">The target position.</param>
    /// <param name="yRot">An optional parameter for setting the Y rotation.</param>
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

    public static bool IsPlayer(Component C) => Exists && C != null && C.gameObject == GameObject;

    #endregion

    #region Events / Callbacks
    /// <summary>
    /// A callback invoked when the player respawns. (Possibly Obsolete?)
    /// </summary>
    public static Action onRespawn;

    /// <summary>
    /// Awake stage of the <see cref="Player"/>, saving the static references and other setup.
    /// </summary>
    public void Awake()
    {
        GameObject = gameObject;
        Transform = transform;
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
        Health.Initialize();
        Ammo.Initialize();
        Currency.Initialize();

        _activeState = ActivityStates.Active;

#if UNITY_EDITOR
        Input.Debug.GodMode.performed += (_) =>
        {
            SaveData.Current.playerStats.upgrades = Upgrades.Debug();
        };
#endif
        fallDownPitTime = Health.playerObject.inFallDownPitTime;
        deathTime = Health.playerObject.inDeathTime;
    }
    #endregion

    #region Models (Health / Ammo / Currency)
    /// <summary>
    /// The Model part of the MVC pattern for Player Health, not to be confused with <see cref="PlayerHealth"/> or <see cref="UIHUDSystem"/>.
    /// </summary>
    public static class Health
    {
        private static int current;
        private static int max;

        public static void Initialize()
        {
            playerObject = GameObject.GetComponent<PlayerHealth>();
            max = SaveData.Current.playerStats.maxHealth;
            current = max;
        }
        public static PlayerHealth playerObject;


        public static int Current
        {
            get => current;
            set
            {
                if(value > max) value = max;
                if(current == value) return;

                current = value;
                updateHealth?.Invoke();
            }
        }
        public static int Max
        {
            get => max;
            set
            {
                if (max == value) return;

                max = value;
                SaveData.Current.playerStats.maxHealth = value;
                updateMaxHealth?.Invoke();
            }
        }

        public static Action updateHealth;
        public static Action updateMaxHealth;
    }

    /// <summary>
    /// The Model part of the MVC pattern for Player Ammo, not to be confused with <see cref="PlayerRanged"/> or <see cref="UIHUDSystem"/>.
    /// </summary>
    public static class Ammo
    {
        private static int current;
        private static int max;

        public static void Initialize()
        {
            playerObject = GameObject.GetComponent<PlayerRanged>();
            max = SaveData.Current.playerStats.maxAmmo;
            current = max;
        }
        public static PlayerRanged playerObject;


        public static int Current
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
        public static int Max
        {
            get => max;
            set
            {
                if (max == value) return;

                max = value;
                SaveData.Current.playerStats.maxAmmo = value;
                updateMaxAmmo?.Invoke();
            }
        }

        public static Action updateAmmo;
        public static Action updateMaxAmmo;
    }

    /// <summary>
    /// The Model part of the MV pattern for Player Ammo, not to be confused with <see cref="UIHUDSystem"/>.
    /// </summary>
    public static class Currency
    {
        private static int current;
        public static void Initialize()
        {
            current = SaveData.Current.playerStats.currency;
        }
        public static int Current
        {
            get => current;
            set
            {
                if (current == value) return;
                current = value;
                SaveData.Current.playerStats.currency = value;
                updateCurrency?.Invoke();
            }
        }
        public static Action updateCurrency;
    }
    #endregion

    #region Death / Respawn Sequence
    static float fallDownPitTime;
    static float deathTime;
    static CoroutinePlus deathCoroutine;

    /// <summary>
    /// Begins the <see cref="Player"/> death sequence.
    /// </summary>
    public static void Death()
    {
        DeathOrPit();
        Enum().Begin(Gameplay.Instance);
        static IEnumerator Enum()
        {
            yield return WaitFor.SecondsRealtime(fallDownPitTime + 1);
            yield return Overlay.OverGameplay.GameOverAnim();
            yield return WaitFor.SecondsRealtime(deathTime);

            RoomManager.TransitionStyle = new()
            {
                FadeOutRoutine = Overlay.OverGameplay.BasicFadeOutWait(1f),
                FadeInRoutine = Overlay.OverGameplay.BasicFadeInWait(1f),
                //PreFadeInAction = Overlay.OverGameplay.Reset
            };

            //Note "Overlay.OverGameplay.Reset() used to be called just after the FadeOut. Not sure why. If necessary, uncomment the above line."

            Gameplay.Death();
        }
    }
    /// <summary>
    /// Begins an altered <see cref="Player"/> death sequence for when falling into a pit.
    /// </summary>
    public static void PitFall()
    {
        DeathOrPit();
        Enum().Begin(Gameplay.Instance);
        static IEnumerator Enum()
        {
            yield return WaitFor.SecondsRealtime(fallDownPitTime);

            RoomManager.TransitionStyle = new()
            {
                FadeOutRoutine = Overlay.OverGameplay.BasicFadeOutWait(1f),
                FadeInRoutine = Overlay.OverGameplay.BasicFadeInWait(1f),
                //PreFadeInAction = Overlay.OverGameplay.Reset
            };            
            //Note "Overlay.OverGameplay.Reset() used to be called just after the FadeOut. Not sure why. If necessary, uncomment the above line."

            Gameplay.Respawn();
        }
    }
    private static void DeathOrPit()
    {
        Vector3 targetVelocity = MovementBody.velocity;
        Audio.PlayOneShot("Death");
        StateMachine.Ragdoll.Enter();
        MovementBody.velocity = Vector3.zero;
        RagdollHandler.State = RagdollHandler.States.Thrown;
        RagdollHandler.SetVelocity(targetVelocity * 0.75f);
        Animator.enabled = false;
    }
    #endregion

    void OnDestroy()
    {
        _activeState = ActivityStates.Null;
    }
}
