using RageRooster.Player;
using SLS.StateMachineH.Signals;
using UnityEngine;
using static RageRooster.Core.IPlayer;

namespace RageRooster.Player
{
    /// <summary>
    /// Static context for direct access to Player components and helpers without getter calls.
    /// </summary>
    public static class Player
    {
        internal static void Initialize(PlayerRoot root)
        {
            Root = root;
            Transform = root.transform;
            GameObject = root.gameObject;
            StateMachine = root.GetComponent<PlayerStateMachine>();
            Health = root.GetComponent<PlayerHealth>();
            MovementBody = root.GetComponent<PlayerMovementBody>();
            Collider = root.GetComponent<CapsuleCollider>();
            Controller = root.GetComponent<PlayerController>();
            Ranged = root.GetComponent<PlayerRanged>();
            Grabber = root.GetComponent<PlayerGrabber>();
            Animator = root.GetComponent<Animator>();
            Audio = root.GetComponent<AudioCaller>();
            RagdollHandler = root.GetComponent<RagdollHandler>();
            TargetingManager = root.GetComponent<TargetingManager>();
            SignalManager = root.GetComponent<SLS.StateMachineH.Signals.SignalManager>();

            _activeState = ActivityStates.Active;
        }

        internal static void Clear()
        {
            Root = null;
            Transform = null;
            GameObject = null;
            StateMachine = null;
            Health = null;
            MovementBody = null;
            Collider = null;
            Controller = null;
            Ranged = null;
            Grabber = null;
            Animator = null;
            Audio = null;
            RagdollHandler = null;
            TargetingManager = null;
            SignalManager = null;

            _activeState = ActivityStates.Null;
        }


        #region Component References
        public static Transform Transform { get; private set; }
        public static GameObject GameObject { get; private set; }
        public static PlayerStateMachine StateMachine { get; private set; }
        public static PlayerHealth Health { get; private set; }
        public static SignalManager SignalManager { get; private set; }
        public static PlayerMovementBody MovementBody { get; private set; }
        public static CapsuleCollider Collider { get; private set; }
        public static PlayerController Controller { get; private set; }
        public static PlayerRanged Ranged { get; private set; }
        public static PlayerGrabber Grabber { get; private set; }
        public static TargetingManager TargetingManager { get; private set; }
        public static Animator Animator { get; private set; }
        public static AudioCaller Audio { get; private set; }
        public static RagdollHandler RagdollHandler { get; private set; }
        public static PlayerRoot Root { get; private set; }
        public static PlayerStats Stats => PlayerStats.Active;
        #endregion

        #region GameplayState
        public static ActivityStates ActivityState
        {
            get => _activeState;
            set
            {
                if (_activeState == value || value is ActivityStates.Null || _activeState is ActivityStates.Null)
                    return;

                _activeState = value;

                GameObject.SetActive(value != ActivityStates.Invisible);
                StateMachine.enabled = value is ActivityStates.Active;

                MovementBody.enabled = value is ActivityStates.Active or ActivityStates.Dying;
                Controller.enabled = value is ActivityStates.Active;
                Animator.enabled = value is ActivityStates.Active or ActivityStates.Cutscene;
                Ranged.enabled = value is ActivityStates.Active;
            }
        }

        private static ActivityStates _activeState = ActivityStates.Null;

        public static bool Exists => ActivityState is not ActivityStates.Null;
        public static bool Active => ActivityState is ActivityStates.Active;
        public static bool Paused => ActivityState is ActivityStates.Paused;
        public static bool InCutscene => ActivityState is ActivityStates.Cutscene;
        public static bool Dying => ActivityState is ActivityStates.Dying;
        #endregion

        #region Helper Properties / Methods
        public static Vector3 Position => Exists ? Transform.position : Vector3.zero;
        public static Vector3 Center => Transform.position + Collider.center;
        public static Quaternion Rotation => Transform.rotation;
        public static Vector3 Forward => Transform.forward;

        public static MonoBehaviour CurrentVent { get; set; }

        public static float DistanceFrom(Vector3 pos) => Exists ? Vector3.Distance(Position, pos) : 999999f;

        public static void InstantMove(Vector3 newPosition, float? yRot = null)
        {
            if (!Exists) return;
            Vector3 camDelta = newPosition - Transform.position;
            MovementBody.Position = newPosition;
            if (yRot != null) MovementBody.Direction.Rotation = new(0, yRot.Value, 0);
            StateMachine.ResetState();
            Cameras.currentVirtualCamera.PreviousStateIsValid = false;
            Cameras.currentVirtualCamera.OnTargetObjectWarped(Transform, camDelta);
            MovementBody.Velocity.ZeroOut();
        }

        public static bool Owns(Component C) => Exists && C != null && C.gameObject == GameObject;

        public static void Death()
        {
            if (Root != null) Root.Death();
        }

        public static void Respawn()
        {
            if (Root != null) Root.Respawn();
        }
        #endregion
    }
}