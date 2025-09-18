using RageRooster.Systems.SaveSystem;
using System;
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
    public static Animator Animator { get; private set; }
    public static AudioCaller Audio { get; private set; }
    public static RagdollHandler RagdollHandler { get; private set; }

    public static Vector3 Position => Transform.position;
    public static Quaternion Rotation => Transform.rotation;
    public static Vector3 Forward => Transform.forward;
    public static Vector3 EularAngles => Transform.eulerAngles;

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
            max = SaveFile.Current.playerStats.maxHealth;
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
                SaveFile.Current.playerStats.maxHealth = value;
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
            max = SaveFile.Current.playerStats.maxAmmo;
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
                SaveFile.Current.playerStats.maxAmmo = value;
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
            current = SaveFile.Current.playerStats.currency;
        }
        public static int Current
        {
            get => current;
            set
            {
                if (current == value) return;
                current = value;
                SaveFile.Current.playerStats.currency = value;
                updateCurrency?.Invoke();
            }
        }
        public static Action updateCurrency;
    }


    public static float fallDownPitTime { get; protected set; }
    public static float deathTime { get; protected set; }
    static CoroutinePlus deathCoroutine;
    public static void Death(bool justPit = false)
    {
        CoroutinePlus.Begin(ref deathCoroutine, Enum(justPit), Gameplay.Instance);
        IEnumerator Enum(bool justPit)
        {
            Vector3 targetVelocity = MovementBody.velocity;
            Audio.PlayOneShot("Death");
            StateMachine.ragDollState.Enter();
            MovementBody.velocity = Vector3.zero;
            RagdollHandler.SetState(EntityState.RagDoll);
            RagdollHandler.SetVelocity(targetVelocity * 0.75f);
            Animator.enabled = false;

            yield return WaitFor.SecondsRealtime(justPit ? fallDownPitTime : fallDownPitTime + 1);

            if (!justPit)
            {
                yield return Overlay.OverGameplay.GameOverAnim();
                yield return WaitFor.SecondsRealtime(deathTime);
                yield return Overlay.OverMenus.BasicFadeOutWait(1f);
                Overlay.OverGameplay.Reset();
                yield return Gameplay.Death();
            }
            else
            {
                yield return Overlay.OverMenus.BasicFadeOutWait(1f);
                Overlay.OverGameplay.Reset();
                yield return Gameplay.Respawn();
            }
        }
    }
}
