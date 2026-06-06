using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace RageRooster.Physics
{
    /// <summary>
    /// Abstract base class for movement resolvers. A resolver is responsible for translating a proposed movement vector into collisions, sliding, landing and other movement effects for its owning <see cref="PhysicsBody"/>.
    /// </summary>
    [System.Serializable]
    public abstract class PhysicsResolver : Polymorph
    {
        #region Relations

        /// <summary>
        /// Initialize this resolver with its owning <see cref="PhysicsBody"/>.
        /// This must be called by the owner during Awake/Start before using the resolver.
        /// </summary>
        /// <param name="body">The owning PhysicsBody.</param>
        public void Init(PhysicsBody body) => this.body = body;

        /// <summary>
        /// The owning PhysicsBody instance. Available after <see cref="Init"/> is called.
        /// </summary>
        public PhysicsBody body { get; private set; }

        /// <summary>
        /// Convenience properties that forward to the owning body. These provide quick
        /// access to common state frequently used by resolvers.
        /// </summary>
        protected Vector3 Position => body.Position;
        protected Velocity stepZeroVelocity => body.velocity;
        protected GroundState ground => body.ground;
        protected AnchorPoint anchor => body.ground.anchor;
        protected Direction direction => body.direction;

        #endregion

        /// <summary>
        /// Tracks how many internal projection steps this resolver has executed in
        /// the current FixedUpdate cycle. Consumers may use this to prevent runaway
        /// recursion when a resolver delegates further movement processing.
        /// </summary>
        [NonSerialized] public int step = 0;

        /// <summary>
        /// Lifecycle hooks and the main Move contract for resolvers.
        /// </summary>
        public virtual void Start() { }
        /// <summary>
        /// Called when this resolver becomes the active resolver for a PhysicsBody.
        /// </summary>
        public virtual void Enter() { }
        /// <summary>
        /// Called when this resolver is no longer active for a PhysicsBody.
        /// </summary>
        public virtual void Exit() { }
        /// <summary>
        /// Called before the main resolver Move invocation for per-frame setup.
        /// </summary>
        public virtual void FixedUpdateFormer() { }
        /// <summary>
        /// Called after the main resolver Move invocation for per-frame teardown.
        /// </summary>
        public virtual void FixedUpdateLatter() { }
        /// <summary>
        /// Process the supplied movement vector (<paramref name="stepVelocity"/>)
        /// for this resolver's domain. The implementation is responsible for
        /// performing collision sweeps, updating body position and optionally
        /// delegating remaining movement to the next resolver via the owning
        /// body's resolver selection.
        /// </summary>
        /// <param name="stepVelocity">The movement vector to process, typically velocity * deltaTime.</param>
        public abstract void Move(Vector3 stepVelocity);

        /// <summary>
        /// A resolver representing the famed "Collide and Slide" algorithm. This resolver performs a single collision sweep for the proposed movement vector, moves the body to the point of impact (or full distance if no collision), and then delegates remaining movement along the surface normal of the collision.
        /// </summary>
        [System.Serializable]
        public class CollideAndSlide : PhysicsResolver
        {
            [Tooltip("The maximum amount of steps this resolver allows.")]
            [SerializeField] protected int maxSteps = 6;
            [Tooltip("The distance of the buffer that will be used in sweep checking.")]
            [SerializeField] float checkBuffer = 0.1f;
            [Tooltip("A Layermask for solid ground.")]
            [SerializeField] LayerMask validGroundMask;

            public override void Move(Vector3 stepVelocity)
            {
                if (stepVelocity == Vector3.zero) return;

                if (ground) stepVelocity.y = 0;
                stepVelocity = stepVelocity.ProjectAndScale(anchor.normal);

                float stopDistance = -1;
                Vector3 nextNormal = Vector3.zero;
                bool scaleByDot = false;
                bool negateVerticalLefover = false;

                // Sweep for any obstacle in the trajectory (ignore flat-floor hits when moving purely horizontally).
                bool sweepHit = body.Sweep(stepVelocity, out RaycastHit hit, checkBuffer)
                    && !(stepVelocity.y == 0 && hit.normal == Vector3.up);

                if (!sweepHit) //No Hit
                {
                    /*
                    // Keep platform lock behavior for grounded movement (attempt to detect unreachable edges and snap behavior).
                    if (lockToNavMesh)
                    {
                        Vector3 platformCheckDistance = stepVelocity.normalized * platformDetectionFactor;

                        if (!SweepBody(Vector3.down * checkBuffer, out RaycastHit platformCheckHit,
                            checkBuffer, Position + platformCheckDistance))
                        {
                            Vector3 reachAroundPos = Position + (platformCheckDistance * 1.01f) - (Vector3.up * Collider.height / 2);
                            if (SweepBody(platformCheckDistance.XZ() * -2f, out RaycastHit reachAroundResult, 0, reachAroundPos))
                            {
                                nextNormal = -reachAroundResult.normal.XZ();
                                Plane P = new(nextNormal, reachAroundResult.point + (nextNormal * .6f));
                                P.Raycast(new(Position, stepVelocity), out float hitDistance);
                                if (hitDistance <= stepVelocity.magnitude) stopDistance = hitDistance;

                                scaleByDot = true;
                                AddDebugText($"Platform Locked onto non-NavMesh Platform, nextNormal: {nextNormal}");
                            }
                            else AddDebugText("Walking off platform when not allowed but reach around check failed. Failsafe situation, report to CJ.");
                        }
                    }*/


                    if (stopDistance == -1)
                    {
                        // Snap down to a slightly lower ground if detected (small ledge correction).
                        if (body.ground.Check(out _, out RaycastHit groundCast, true) && groundCast.normal != anchor.normal)
                        {
                            Ray cornerCheckRay = new(groundCast.barycentricCoordinate + new Vector3(0, .1f, 0), Vector3.down);
                            bool different = groundCast.collider.Raycast(cornerCheckRay, out RaycastHit baryHit, .11f)
                                && baryHit.normal != groundCast.normal;

                            if (groundCast.distance >= float.Epsilon && groundCast.distance <= checkBuffer && !different)
                            {
                                body.Position += Vector3.down * groundCast.distance;
                                ground.Land(groundCast);
                            }
                        }
                    }
                }
                else // Hit
                {
                    stopDistance = hit.distance;
                    nextNormal = hit.normal;

                    if (Mathf.Approximately(hit.normal.y, 0)) // Hit a Wall
                    {
                        scaleByDot = true;
                        negateVerticalLefover = true;
                        nextNormal = nextNormal.XZ().normalized;
                    }
                    else if (hit.normal.y > 0 && !ground.WithinSlopeAngle(hit.normal)) // Hit a steep slope
                    {
                        scaleByDot = true;
                        negateVerticalLefover = true;
                        nextNormal = nextNormal.XZ().normalized;
                    }

                    if (anchor.normal.y > 0 && hit.normal.y < 0) FloorCeilingLock(anchor, hit.normal);
                    else if (anchor.normal.y < 0 && hit.normal.y > 0) FloorCeilingLock(hit.normal, anchor);

                    void FloorCeilingLock(Vector3 floorNormal, Vector3 ceilingNormal)
                    {
                        scaleByDot = true;
                        nextNormal = floorNormal.y != floorNormal.magnitude ? floorNormal : ceilingNormal;
                    }

                    // If we hit a valid ground surface and are moving downwards or flat, land on it.
                    if (hit.normal.y > 0 && ground.WithinSlopeAngle(hit.normal) && stepVelocity.y <= 0) ground.Land(hit);
                }

                Vector3 snapToSurface = stopDistance != -1 ? stepVelocity.normalized * stopDistance : stepVelocity;

                // Make sure we aren't moving off into the void at the destination
                if (!body.Sweep(Vector3.down * 5000, out _, checkBuffer, snapToSurface, QueryTriggerInteraction.Collide)) return;

                body.Position += snapToSurface;

                if (stopDistance < 0 || ++step >= maxSteps) return;
                else if (body.LastChanceStopper(stepVelocity.XZ(), nextNormal.XZ())) return;

                Vector3 leftover = stepVelocity - snapToSurface;
                if (negateVerticalLefover)
                {
                    leftover.y = 0;
                    ground.Land(hit);
                }
                Vector3 newDir = leftover.ProjectAndScale(nextNormal);
                if (scaleByDot) newDir *= Vector3.Dot(leftover.normalized, nextNormal) + 1;

                body.NextResolver?.Move(newDir);
            }
        }

        /// <summary>
        /// A resolver based on the <see cref="CollideAndSlide"/> resolver but with all grounded-movement-related logic removed.
        /// </summary>
        [System.Serializable]
        public class Air : PhysicsResolver
        {
            [Tooltip("The distance of the buffer that will be used in sweep checking.")]
            [SerializeField] float checkBuffer = 0.1f;
            [Tooltip("The default gravity value that will be applied to this resolver on Start().")]
            [SerializeField] float defaultGravity = 9.8f;
            [Tooltip("Whether this resolver should automatically apply gravity each frame. If false, gravity must be applied manually by calling ApplyGravity().")]
            [SerializeField] bool autoApplyGravity = false;

            public override void Start() => gravity = defaultGravity;

            public override void Move(Vector3 stepVelocity)
            {
                if (stepVelocity == Vector3.zero) return;

                if (ground) stepVelocity.y = 0;
                stepVelocity = stepVelocity.ProjectAndScale(anchor.normal);

                float stopDistance = -1;
                Vector3 nextNormal = Vector3.zero;
                bool land = false;

                // Sweep for any obstacle in the trajectory (ignore flat-floor hits when moving purely horizontally).
                bool sweepHit = body.Sweep(stepVelocity, out RaycastHit hit, checkBuffer)
                    && !(stepVelocity.y == 0 && hit.normal == Vector3.up);

                if (sweepHit) //Hit
                {
                    stopDistance = hit.distance;
                    nextNormal = hit.normal;

                    if (Mathf.Approximately(hit.normal.y, 0)) // Hit a Wall
                    {
                        nextNormal = nextNormal.XZ().normalized;
                    }
                    else if (hit.normal.y > 0)
                    {
                        if (ground.WithinSlopeAngle(hit.normal)) land = true; // hit landable ground.
                        else // hit steep slope
                        {
                            nextNormal = nextNormal.XZ().normalized;
                            //This ^^^ feels wrong but I'm leaving it in for now. Do Testing Please.
                        }
                    }
                    else
                    {
                        if (ground.WithinSlopeAngle(-hit.normal)) //Hit a ceiling
                        {
                            //BONK (Implement later)
                        }
                        else //Hit a steep upward slope: slide against it.
                        {
                            nextNormal = nextNormal.XZ().normalized;
                            //This ^^^ feels wrong but I'm leaving it in for now. Do Testing Please.
                        }
                    }

                    // If we hit a valid ground surface and are moving downwards or flat, land on it.
                    if (hit.normal.y > 0 && ground.WithinSlopeAngle(hit.normal) && stepVelocity.y <= 0) ground.Land(hit);
                }
                else
                {
                    if (!body.Sweep(Vector3.down * 5000, out _, checkBuffer, Position + stepVelocity, QueryTriggerInteraction.Collide))
                    {
                        body.velocity.ZeroOut();
                        body.velocity.y = -1f;
                        // If going forward will put this body over the void, don't move at all.
                    }
                    //Regardless, end loop as nothing new is gonna happen.
                    return;
                }


                Vector3 snapToSurface = stopDistance != -1 ? stepVelocity.normalized * stopDistance : stepVelocity;

                // Make sure we aren't moving off into the void at the destination


                body.Position += snapToSurface;

                if (stopDistance < 0) return;
                else if (body.LastChanceStopper(stepVelocity.XZ(), nextNormal.XZ())) return;

                Vector3 leftover = stepVelocity - snapToSurface;
                if (land && body.Resolvers > 1) // Don't do landing logic if no ground-based resolvers exist.
                {
                    leftover.y = 0;
                    ground.Land(hit);
                }
                Vector3 newDir = leftover.ProjectAndScale(nextNormal);
                newDir *= Vector3.Dot(leftover.normalized, nextNormal) + 1;

                body.NextResolver?.Move(newDir);
            }

            public override void FixedUpdateLatter() { if (autoApplyGravity) ApplyGravity(); }

            /// <summary>
            /// Runs the calculations to automatically apply the current gravity to this body.
            /// </summary>
            public void ApplyGravity() => body.velocity.u -= gravity * Time.fixedDeltaTime;

            /// <summary>
            /// The active gravity value. (Inverted. y=1 is down.)
            /// </summary>
            private float gravity = 9.8f;

            /* 3D Gravity (Not Necessary for this project.)
            /// <summary>
            /// The active gravity value. (Inverted. y=1 is down.)
            /// </summary>
            [NonSerialized] private Vector3 gravity = new(0, 9.8f, 0);

            /// <summary>
            /// Returns the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
            /// </summary>
            public Vector3 Get3DGravity() => gravity;
            /// <summary>
            /// Returns the current gravity value on the Y axis. (Inverted. 1 is downwards, -1 is upwards.)
            /// </summary>
            public float GetGravity() => gravity.y;

            /// <summary>
            /// Sets the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
            /// </summary>
            /// <param name="newGravity">The new gravity value.</param>
            public void SetGravity(Vector3 newGravity) => gravity = newGravity;
            /// <summary>
            /// Sets the current gravity value on the Y axis. (Inverted. 1 is downwards, -1 is upwards.)
            /// </summary>
            /// <param name="newGravity">The new gravity value.</param>
            public void SetGravity(float newGravity) => gravity = new(0, newGravity, 0);
            /// <summary>
            /// Sets the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
            /// </summary>
            /// <param name="newX"> The new gravity value on the x axis. (1 = left.) </param>
            /// <param name="newY"> The new gravity value on the y axis. (1 = down.) </param>
            /// <param name="newZ"> The new gravity value on the z axis. (1 = back.) </param>
            public void SetGravity(float newX, float newY, float newZ) => gravity = new(newX, newY, newZ);
            */

        }

        /// <summary>
        /// A resolver specifically for use in NavMeshes. This resolver uses a NavMeshAgent to perform movement and pathfinding, and is designed to be used as the final resolver in the chain for characters that should be fully NavMesh-driven. It includes logic to attempt to snap to the NavMesh if the agent becomes ungrounded, and can optionally lock movement to the NavMesh surface when navigating off ledges or small platforms.
        /// </summary>
        [System.Serializable]
        public class NavMesh : PhysicsResolver
        {
            [Tooltip("The NavMeshAgent component used for movement and pathfinding.")]
            [field: SerializeField] public NavMeshAgent NavAgent { get; private set; }
            [Tooltip("The maximum amount of steps this resolver allows when processing movement. This is to prevent runaway recursion when the resolver delegates back to itself due to being ungrounded.")]
            [SerializeField] protected int maxSteps = 3;
            [Tooltip("Whether this resolver should attempt to lock movement to the NavMesh surface when navigating off ledges or small platforms. This can help prevent characters from unintentionally walking off of small platforms, but may cause unwanted snapping behavior in some cases.")]
            [SerializeField] bool lockToNavMesh = true;
            [Tooltip("The distance within which the resolver will attempt to snap to the NavMesh if the agent becomes ungrounded. This should generally be set to a value slightly larger than the expected maximum step height of the character.")]
            [field: SerializeField] public float detectionRange { get; private set; } = .35f;


            /// <summary>
            /// Moves body via Nav Mesh.
            /// </summary>
            public override void Move(Vector3 stepVelocity)
            {

                if (stepVelocity == Vector3.zero) return;

                if (!NavAgent.Raycast(Position + stepVelocity, out NavMeshHit hit))
                {
                    NavAgent.Move(stepVelocity);
                }
                else
                {
                    Vector3 snapToSurface = stepVelocity.normalized * hit.distance;

                    NavAgent.Move(snapToSurface);

                    if (++step >= maxSteps) return;

                    Vector3 leftover = stepVelocity - snapToSurface;
                    if (lockToNavMesh) leftover = leftover.ProjectAndScale(hit.normal);
                    else body.UpdateNextResolver(body.GetResolver<CollideAndSlide>());

                    body.NextResolver?.Move(leftover);
                }

            }

            public override void Enter()
            {
                NavAgent.enabled = true;

                if (!UnityEngine.AI.NavMesh.SamplePosition(Position, out NavMeshHit sampleHit, detectionRange, NavAgent.areaMask)
                    || !!NavAgent.isOnNavMesh)
                {
                    body.UpdateNextResolver(body.GetResolver<CollideAndSlide>());
                    return;
                }
                destinationDriven = false;
                // Place agent internal position on the navmesh
                NavAgent.Warp(sampleHit.position);
                NavAgent.nextPosition = sampleHit.position;
                // Place the RB at the same surface + baseOffset so visuals/physics line up
                body.PositionForce = sampleHit.position + Vector3.up * NavAgent.baseOffset;

                // We will manage character position ourselves (RB) and use NavAgent for pathfinding only.
                NavAgent.enabled = true;
            }
            public override void Exit()
            {
                destinationDriven = false;
                NavAgent.enabled = false;
            }

            /// <summary>
            /// Whether this resolver is currently controlling movement via NavAgent destination. This is used to track whether the resolver should be outputting the NavAgent's desired velocity and actively moving towards the destination, or if it should be idle and allow other resolvers to control movement until a new destination is set. This is necessary because NavMeshAgents will continue to output a desired velocity even when they are not actively navigating towards a destination, which can cause unwanted movement if not properly managed.
            /// </summary>
            private bool destinationDriven = false;

            /// <summary>
            /// Getter Variant, just returns current Destination.
            /// </summary>
            /// <returns>The current NavDestination, will be zero if there is none.</returns>
            public Vector3 NavDestination() => destinationDriven ? NavAgent.destination : Vector3.zero;

            /// <summary>
            /// Setter Value Variant. Sets Destination and activates Destination-driven behavior, if possible.
            /// </summary>
            /// <param name="value"></param>
            /// <returns>Success.</returns>
            public bool NavDestination(Vector3 value)
            {
                destinationDriven = true;
                NavAgent.destination = value;
                return true;
            }
            /// <summary>
            /// Setter Activation Variant. Activates/Deactivates Destination-driven Behavior. Destination value is optional to allow False Setting.
            /// </summary>
            public bool NavDestination(bool value, Vector3 destinationValue = default)
            {
                if (value)
                {
                    destinationDriven = true;
                    NavAgent.destination = destinationValue;
                    return true;
                }
                else
                {
                    destinationDriven = false;
                    NavAgent.ResetPath();
                    // keep agent disabled? existing code leaves NavAgent.enabled as-is; we keep existing behavior
                    return false;
                }
            }
            /// <summary>
            /// Getter Bool with Output Variant. Returns whether Destination-driven Behavior is active and outs the destination value.
            /// </summary>
            public bool NavDestination(out Vector3 result)
            {
                result = NavAgent.destination;
                return destinationDriven;
            }

            public override void FixedUpdateFormer()
            {
                if (destinationDriven)
                {
                    body.direction.Set(NavAgent.desiredVelocity, NavAgent.angularSpeed * Time.fixedDeltaTime);
                    NavAgent.velocity = Vector3.zero;

                    stepZeroVelocity.Global = (Vector3.Dot(NavAgent.desiredVelocity, direction) + 1) * NavAgent.desiredVelocity.magnitude * (Vector3)direction;
                    if (NavAgent.remainingDistance < 0.1f) NavDestination(false);
                }
            }
        }
    }
}