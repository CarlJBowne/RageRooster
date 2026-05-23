using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Cinemachine.Utility;
using RageRooster.Systems.SaveSystem;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using Utilities.Xtensions;
using Utilities.Xtensions.Unity;

namespace RageRooster.Physics
{
    /// <summary>
    /// Core physics body component that owns per-entity physics state and delegates movement
    /// resolution to modular <see cref="PhysicsResolver"/> implementations. <br/>
    /// This component centralizes the Rigidbody/Collider/NavMeshAgent integration, exposes
    /// the high-level physical concepts (velocity, ground state, facing direction), and
    /// coordinates resolver selection and invocation each FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(NavMeshAgent))]
    public class PhysicsBody : MonoBehaviour
    {
        void FixedUpdate()
        {
            Vector3 prePos = Position;

            if (BodyState != BodyStates.Enabled) return;

            RB.linearVelocity = Vector3.zero;
            RB.angularVelocity = Vector3.zero;

            for (int i = 0; i < resolvers.Count; i++) resolvers[i].step = 0;
            NextResolver?.FixedUpdateFormer();

            if (velocity.r != 0f) direction.RotationY += velocity.r * Time.fixedDeltaTime;

            Vector3 stepZeroVelocity = velocity.Global * Time.fixedDeltaTime;

            if (stepZeroVelocity.IsNaN() || stepZeroVelocity.sqrMagnitude > 300) stepZeroVelocity = Vector3.zero;
            if (stepZeroVelocity != Vector3.zero) NextResolver?.Move(stepZeroVelocity);


            if (velocity.y <= 0)
            {
                if (ground.Check(out AnchorPoint groundHit))
                {
                    if (!ground)
                    {
                        ground.Land(groundHit);
                        velocity.y = 0;
                    }
                }
                else if (ground) ground.UnLand(GroundState.Hangtime);
            }

            NextResolver?.FixedUpdateLatter();

        }

        /// <summary>
        /// The current velocity container for this body. Contains both local (f/s/u) and
        /// global (x/y/z) representations and helper methods to keep them in sync.
        /// </summary>
        [field: SerializeField] public Velocity velocity { get; private set; }

        /// <summary>
        /// Current ground state for this body. Tracks whether the body is grounded, the
        /// anchor point (surface normal/point/collider) and exposes checks for ledges and
        /// slope limits.
        /// </summary>
        [field: SerializeField] public GroundState ground { get; private set; }

        /// <summary>
        /// Direction helper that represents the local forward vector used for local
        /// velocity computations and rotation helpers.
        /// </summary>
        [field: SerializeField] public Direction direction { get; private set; }

        #region Resolvers

        [SerializeField] Polymorph.UniqueList<PhysicsResolver> resolvers = new();
        /// <summary>
        /// Gets the <see cref="PhysicsResolver"/> of type T if it exists on this body.
        /// </summary>
        public PhysicsResolver GetResolver<T>() where T : PhysicsResolver => resolvers.Get<T>();
        /// <summary>
        /// Attempts to Get the <see cref="PhysicsResolver"/> of type T if it exists on this body.
        /// </summary>
        public bool TryGetResolver<T>(out T res) where T : PhysicsResolver => resolvers.TryGet(out res);
        /// <summary>
        /// The number of <see cref="PhysicsResolver"/>s currently attached to this body.
        /// </summary>
        public int Resolvers => resolvers.Count;

        /// <summary>
        /// The currently-active resolver used to process movement for this body. The
        /// value is selected based on the current <see cref="ground"/> state and which
        /// resolvers have been configured on this component.
        /// </summary>
        public PhysicsResolver NextResolver { get; private set; }

        /// <summary>
        /// Re-evaluates which resolver should be used for movement based on the current
        /// ground state and available resolver implementations attached to this body.
        /// If the resolver changes this method will call <see cref="PhysicsResolver.Exit"/>
        /// on the previous resolver and <see cref="PhysicsResolver.Enter"/> on the new one.
        /// </summary>
        public void UpdateNextResolver()
        {
            PhysicsResolver prevResolver = NextResolver;

            NextResolver = ground
                ? TryGetResolver(out PhysicsResolver.NavMesh nav) && NavMesh.SamplePosition(Position, out _, nav.detectionRange, NavMesh.AllAreas) ? GetResolver<PhysicsResolver.NavMesh>()
                    : GetResolver<PhysicsResolver.CollideAndSlide>()
                : GetResolver<PhysicsResolver.Air>();

            if (prevResolver != NextResolver)
            {
                prevResolver.Exit();
                NextResolver.Enter();
            }
        }
        /// <summary>
        /// Force-sets the active resolver. This is intended for external callers that need
        /// to temporarily override resolution behavior (for example gameplay-driven modes).
        /// </summary>
        /// <param name="force">The resolver instance to activate.</param>
        public void UpdateNextResolver(PhysicsResolver force) => NextResolver = force;

        #endregion

        /// <summary>
        /// Casts the Rigidbody in a direction to check for collision using SweepTest. (Includes optional buffer)
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="hit">The resulting Hit.</param>
        /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
        /// <param name="tempOrigin">An optional temporary origin to move the Rigidbody to before the Sweep Test.</param>
        /// <param name="queryTriggerInteraction">Override to include trigger colliders in the Sweep Test.</param>
        /// <returns>Whether anything was Hit.</returns>
        /// <summary>
        /// Performs a sweep test using the internal Rigidbody to determine whether this
        /// body would collide when translated by <paramref name="offset"/>. Optionally
        /// supports a temporary origin and a buffer distance to shrink the effective start
        /// location for the sweep.
        /// </summary>
        /// <param name="offset">The desired translation vector to sweep along.</param>
        /// <param name="hit">Outputs the first RaycastHit detected by the sweep (if any).</param>
        /// <param name="buffer">A small buffer to back the test origin up along <paramref name="offset"/>. Defaults to 0.</param>
        /// <param name="tempOrigin">An optional temporary origin to perform the sweep from instead of the current RB position.</param>
        /// <param name="queryTriggerInteraction">Whether the sweep should hit trigger colliders. Defaults to Ignore.</param>
        /// <returns>True if the sweep detected a collider, otherwise false.</returns>
        public bool Sweep(Vector3 offset, out RaycastHit hit, float buffer = 0, Vector3? tempOrigin = null, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            Vector3 originalPos = RB.position;

            if (tempOrigin.HasValue && buffer > 0) RB.MovePosition(tempOrigin.Value - (offset.normalized * buffer));
            else if (tempOrigin.HasValue) RB.MovePosition(tempOrigin.Value);
            else if (buffer > 0) RB.MovePosition(RB.position - (offset.normalized * buffer));

            bool result = RB.SweepTest(offset.normalized, out hit, offset.magnitude + buffer, queryTriggerInteraction);

            if (tempOrigin.HasValue || buffer > 0) RB.MovePosition(originalPos);

            hit.distance = (hit.distance - buffer).Min(0);
            return result;
        }

        #region LifeCycle and Components

        [field: SerializeField, RelatedComponent(true)] public Rigidbody RB { get; private set; }
        /// <summary>
        /// The <see cref="CapsuleCollider"/> component attached to this <see cref="CharacterMovementBody"/>.
        /// </summary>
        [field: SerializeField, RelatedComponent(true)] public Collider Collider { get; private set; }

        /// <summary>
        /// Unity Reset callback used to initialize related components when the component
        /// is first added or when Reset is invoked in the editor.
        /// </summary>
        void Reset()
        {
            RB = GetComponent<Rigidbody>();
            Collider = GetComponent<CapsuleCollider>();
        }

        /// <summary>
        /// Unity Awake lifecycle event. Ensures required components exist, initializes
        /// subcomponents and resolves any initial ground snap.
        /// </summary>
        void Awake()
        {
            if (RB == null) RB = GetComponent<Rigidbody>();
            if (Collider == null) Collider = GetComponent<Collider>();

            ground.Init(this);
            if (ground.InstantSnapToFloor(out RaycastHit hit)) ground.Land(hit);

            direction.Init(this);
            velocity.Init(this);

            for (int i = 0; i < resolvers.Count; i++) resolvers[i].Start();
        }

        /// <summary>
        /// Called when the component is enabled.
        /// </summary>
        /// <summary>
        /// Unity OnEnable lifecycle event. Restores the active physics state if the body
        /// was previously turned off.
        /// </summary>
        void OnEnable() { if (_rbState == BodyStates.OFF) BodyState = BodyStates.Enabled; }
        /// <summary>
        /// Called when the component is disabled.
        /// </summary>
        /// <summary>
        /// Unity OnDisable lifecycle event. Puts the body into the OFF state which makes
        /// the Rigidbody kinematic and disables collision checks.
        /// </summary>
        void OnDisable() => BodyState = BodyStates.OFF;


        /// <summary>
        /// The possible states for a <see cref="CharacterMovementBody"/>.
        /// </summary>
        public enum BodyStates
        {
            Enabled,
            Ragdoll,
            OFF
        }

        /// <summary>
        /// The current state of this <see cref="CharacterMovementBody"/>.
        /// </summary>
        public BodyStates BodyState
        {
            get => _rbState;
            set
            {
                _rbState = value;
                switch (value)
                {
                    case BodyStates.Enabled:
                        RB.isKinematic = false;
                        RB.detectCollisions = true;
                        RB.useGravity = false;
                        Collider.enabled = true;
                        break;
                    case BodyStates.Ragdoll:
                        RB.isKinematic = false;
                        RB.detectCollisions = true;
                        RB.useGravity = true;
                        Collider.enabled = false;
                        break;
                    case BodyStates.OFF:
                        RB.isKinematic = true;
                        RB.detectCollisions = false;
                        RB.useGravity = false;
                        Collider.enabled = false;
                        break;
                }
            }
        }
        BodyStates _rbState = BodyStates.Enabled;

        #endregion LifeCycle

        #region Physicals

        /// <summary>
        /// Gets or sets the position of the character.
        /// </summary>
        public Vector3 Position
        {
            get => BodyState == BodyStates.Enabled
                ? NextResolver is not PhysicsResolver.NavMesh N
                    ? RB.position
                    : N.NavAgent.nextPosition
                : transform.position;
            set
            {
                if (BodyState != BodyStates.Enabled) return;

                if (NextResolver is PhysicsResolver.NavMesh N) N.NavAgent.nextPosition = value;
                else RB.MovePosition(value);
            }
        }

        /// <summary>
        /// Sets the position even if the Rigidbody is kinematic.
        /// </summary>
        /// <param name="newPosition">The new position.</param>
        public Vector3 PositionForce
        {
            set
            {
                transform.position = value;
                RB.position = value;
                RB.MovePosition(value);
            }
        }

        /// <summary>
        /// The center of the collider for this body.
        /// </summary>
        public Vector3 center => Position +
            (Collider is CapsuleCollider cap ? cap.center
            : Collider is BoxCollider box ? box.center
            : Collider is SphereCollider sph ? sph.center
            : Vector3.zero
            );

        /// <summary>
        /// Handles collision events with other objects.
        /// </summary>
        /// <param name="collision">The collision information.</param>
        /// <summary>
        /// Unity collision callback. Used to detect immediate contacts that should
        /// influence vertical velocity and potential landing when coming into contact
        /// with a surface during an airborne state.
        /// </summary>
        /// <param name="collision">Collision information provided by Unity.</param>
        void OnCollisionEnter(Collision collision)
        {
            Vector3 contactNormal = collision.GetContact(0).normal;
            if (!ground && velocity.y > .1f && Vector3.Dot(contactNormal, Vector3.up) < -0.75f) velocity.y = 0;
            else if (!ground && ground.WithinSlopeAngle(contactNormal))
                ground.Land(collision.GetContact(0));

        }

        /// <summary>
        /// Called by <see cref="GroundState"/> when this body lands on a surface.
        /// Override to perform game-specific landing behavior. The default implementation
        /// will re-evaluate the active resolver.
        /// </summary>
        /// <param name="wasntGrounded">True if the body was previously not grounded.</param>
        /// <param name="objectChange">True if the collider surface changed since last ground.</param>
        public virtual void OnLand(bool wasntGrounded, bool objectChange) => UpdateNextResolver();

        /// <summary>
        /// Called by <see cref="GroundState"/> when this body leaves the ground. Override
        /// to perform game-specific airborne entry behavior. The default implementation
        /// will re-evaluate the active resolver.
        /// </summary>
        /// <param name="newValue">The new ground state value being transitioned to.</param>
        public virtual void OnUnLand(GroundState.Values newValue) => UpdateNextResolver();

        [SerializeField] float bonkThreshold = 15;
        public virtual bool LastChanceStopper(Vector3 velocity, Vector3 normal) => false;
        //public override bool LastChanceStopper(Vector3 velocity, Vector3 normal)
        //{
        //    if (Vector3.Angle(velocity, -normal) < bonkThreshold && Player.StateMachine.SendSignal(new("Bonk", 0, true)))
        //    {
        //        this.velocity.Global = Vector3.zero;
        //        return true;
        //    }
        //    return false;
        //}
        //ADD THIS TO PLAYER MOVEMENT BODY ONCE IMPLEMENTED.

        #endregion

        /*
        public T CheckForTypeInFront<T>(Vector3 sphereOffset, float checkSphereRadius)
        {
            Collider[] results = Physics.OverlapSphere(center + transform.TransformDirection(sphereOffset),
                                                       checkSphereRadius);
            foreach (Collider r in results)
                if (r.TryGetComponent(out T result))
                    return result;
            return default;
        }
        public T CheckForTypeInFront<T>()
        {
            Collider[] results = Physics.OverlapSphere(center + transform.TransformDirection(frontCheckDefaultOffset),
                                                       frontCheckDefaultRadius);
            foreach (Collider r in results)
                if (r.gameObject != gameObject && r.TryGetComponent(out T result))
                    return result;
            return default;
        }

        /// <summary>
        /// The default offset for a Front-ways collision Check.
        /// </summary>
        [SerializeField] Vector3 frontCheckDefaultOffset;
        /// <summary>
        /// the default radius for a Front-ways collision Check.
        /// </summary>
        [SerializeField] float frontCheckDefaultRadius;

        private static readonly RaycastHit[] s_capsuleCastResults = new RaycastHit[32];

        
        public bool SweepBodyAlt(Vector3 offset, out RaycastHit hit,
            float buffer = 0, Vector3? tempOrigin = null, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            hit = default;

            // world-space origin for the capsule
            Vector3 originPos = tempOrigin ?? RB.position;
            Vector3 worldCenter = originPos + transform.rotation * Collider.center;

            // account for transform scale when computing radius and height
            Vector3 lossy = transform.lossyScale;
            float radius = Collider.radius * Mathf.Max(lossy.x, lossy.z);
            float height = Mathf.Max(Collider.height * lossy.y, radius * 2f); // ensure valid capsule
            float halfHeight = Mathf.Max(0f, (height / 2f) - radius);

            // capsule endpoints in world space
            Vector3 up = transform.up;
            Vector3 p1 = worldCenter + up * halfHeight;
            Vector3 p2 = worldCenter - up * halfHeight;

            Vector3 dir = offset.normalized;
            float maxDistance = offset.magnitude + buffer;

            // Build a layer mask that includes layers this object's layer can collide with
            // If we actually go down this road, change this to store the layerMask once at the beginning.
            int selfLayer = gameObject.layer;
            int layerMask = 0;
            for (int i = 0; i < 32; i++)
                if (!UnityEngine.Physics.GetIgnoreLayerCollision(selfLayer, i))
                    layerMask |= 1 << i;

            // Perform non-mutating capsule cast using the NonAlloc API
            int count = UnityEngine.Physics.CapsuleCastNonAlloc(p1, p2, radius, dir, s_capsuleCastResults, maxDistance, layerMask, queryTriggerInteraction);

            // Find nearest valid hit that is not this object's collider
            int nearestIndex = -1;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = s_capsuleCastResults[i];
                if (h.collider == null) continue;
                if (h.collider == Collider || h.collider.gameObject == gameObject) continue; // exclude self
                if (h.distance < nearestDist)
                {
                    nearestDist = h.distance;
                    nearestIndex = i;
                }
            }

            // copy and adjust distance for buffer, clamp >= 0
            RaycastHit chosen = s_capsuleCastResults[nearestIndex];
            chosen.distance = Mathf.Max(0f, chosen.distance - buffer);

            hit = chosen;
            return true;
        }
        */

    }
}
