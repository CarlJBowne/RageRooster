using RageRooster.RoomSystem;
using RageRooster.Systems.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(ExecutionOrders.Player), RequireComponent(typeof(PlayerStateMachine))]
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
    public static Animator Animator { get; private set; }
    public static AudioCaller Audio { get; private set; }
    public static RagdollHandler RagdollHandler { get; private set; }

    public static Vector3 Position => Transform.position;
    public static Quaternion Rotation => Transform.rotation;
    public static Vector3 Forward => Transform.forward;
    public static Vector3 EularAngles => Transform.eulerAngles;

    public static float DistanceFrom(Vector3 pos) => Vector3.Distance(Position, pos);

    #region Instance Fields

    public float inFallDownPitTime;
    public float inDeathTime;





    #endregion Instance Fields


    public static Action onRespawn;


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
        Animator = GetComponent<Animator>();
        Audio = GetComponent<AudioCaller>();
        RagdollHandler = GetComponent<RagdollHandler>();
        Health.Initialize();
        Ammo.Initialize();
        Currency.Initialize();

        Exists = true;
        Active = true;

        fallDownPitTime = inFallDownPitTime;
        deathTime = inDeathTime;
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


    public static float fallDownPitTime { get; protected set; }
    public static float deathTime { get; protected set; }
    static CoroutinePlus deathCoroutine;


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
            };
            //RoomManager.PreFadeInAction += () => { Overlay.OverGameplay.Reset(); };
            //Note "Overlay.OverGameplay.Reset() used to be called just after the FadeOut. Not sure why. If necessary, uncomment the above line."

            Gameplay.Death();
        }
    }
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
            };            
            //RoomManager.PreFadeInAction += () => { Overlay.OverGameplay.Reset(); };
            //Note "Overlay.OverGameplay.Reset() used to be called just after the FadeOut. Not sure why. If necessary, uncomment the above line."

            Gameplay.Respawn();
        }
    }
    public static void DeathOrPit()
    {
        Vector3 targetVelocity = MovementBody.velocity;
        Audio.PlayOneShot("Death");
        StateMachine.ragDollState.Enter();
        MovementBody.velocity = Vector3.zero;
        RagdollHandler.SetState(EntityState.RagDoll);
        RagdollHandler.SetVelocity(targetVelocity * 0.75f);
        Animator.enabled = false;
    }
}
