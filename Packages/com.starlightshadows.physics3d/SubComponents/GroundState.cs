using System;
using System.Collections.Generic;
using UnityEngine;

namespace SLS.Physics3D
{
    /// <summary>
    /// <see cref="PhysicsBody"/> Sub-component that tracks whether the body is grounded and relevant information about the ground contact (normal, slope, collider, etc.). This component also provides helper methods for performing ground checks and transitioning between grounded and airborne states.
    /// </summary>
    [System.Serializable]
    public class GroundState : PhysicsSubComponent
    {
        #region Config
        /// <summary>
        /// The buffer (in world units) used when performing a downwards sweep to
        /// determine whether the body is grounded. Small positive values help
        /// tolerate minor geometry gaps and numerical jitter.
        /// </summary>
        [field: SerializeField] public float groundCheckBuffer { get; private set; } = 0.1f;

        /// <summary>
        /// The maximum allowed slope angle (in degrees) a surface can have for the
        /// body to be considered standable. This is compared against the surface
        /// normal using Vector3.Angle to Vector3.up.
        /// </summary>
        [field: SerializeField] public float maxSlopeNormalAngle { get; private set; } = 45f;

        #endregion

        #region Enums

        /// <summary>
        /// The possible states where this <see cref="PhysicsBody"/> is anchored to a collider.
        /// </summary>
        public enum AnchorStates
        {
            /// <summary> This <see cref="PhysicsBody"/> grounded to a NavMesh </summary>
            Grounded,
            /// <summary> This <see cref="PhysicsBody"/> is standing on a non-static surface </summary>
            Standing,
            /// <summary> This <see cref="PhysicsBody"/> is sticking to a wall or ceiling </summary>
            Sticking,
            /// <summary> This <see cref="PhysicsBody"/> is not currently anchored.</summary>
            Airborne,
        }
        /// <summary>
        /// The possible states where this <see cref="PhysicsBody"/> is not anchored to a collider.
        /// </summary>
        public enum AirStates
        {
            /// <summary> This <see cref="PhysicsBody"/> is not currently airborne. </summary>
            Anchored,
            /// <summary> This <see cref="PhysicsBody"/> is going upwards</summary>
            Upward,
            /// <summary> This <see cref="PhysicsBody"/> is losing upward speed</summary>
            Decellerating,
            /// <summary> This <see cref="PhysicsBody"/> is currently has neutral vertical velocity</summary>
            Neutral,
            /// <summary> This <see cref="PhysicsBody"/> is falling</summary>
            Falling,
            /// <summary> This <see cref="PhysicsBody"/> has reached terminal downward velocity</summary>
            Terminus
        }
        /// <summary>
        /// The current Anchored state of this <see cref="PhysicsBody"/>
        /// </summary>
        public AnchorStates AnchorState { get; private set; }
        /// <summary>
        /// The current Airborne state of this <see cref="PhysicsBody"/>
        /// </summary>
        public AirStates AirState { get; private set; }

        #region Conveniences
        public const AnchorStates Grounded = AnchorStates.Grounded;
        public const AnchorStates Standing = AnchorStates.Standing;
        public const AnchorStates Sticking = AnchorStates.Sticking;
        public const AnchorStates Airborne = AnchorStates.Airborne;
        public const AirStates Anchored = AirStates.Anchored;
        public const AirStates Upward = AirStates.Upward;
        public const AirStates Decellerating = AirStates.Decellerating;
        public const AirStates Neutral = AirStates.Neutral;
        public const AirStates Falling = AirStates.Falling;
        public const AirStates Terminus = AirStates.Terminus;
        #endregion

        #region Comparison

        public static implicit operator AnchorStates(GroundState This) => This.AnchorState;
        public static implicit operator AirStates(GroundState This) => This.AirState;

        public static bool operator ==(GroundState This, GroundState other) =>
            This.AnchorState == other.AnchorState && This.AirState == other.AirState;
        public static bool operator !=(GroundState This, GroundState other) => !(This == other);
        public static bool operator ==(GroundState This, AnchorStates other) => This.AnchorState == other;
        public static bool operator !=(GroundState This, AnchorStates other) => This.AnchorState != other;
        public static bool operator ==(GroundState This, AirStates other) => This.AirState == other;
        public static bool operator !=(GroundState This, AirStates other) => This.AirState != other;


        public override bool Equals(object obj) => object.ReferenceEquals(this, obj);
        public override int GetHashCode() => HashCode.Combine(AnchorState, AirState, anchor);

        #endregion

        #endregion

        /// <summary>
        /// The anchor point representing the last ground contact (point, normal, collider).
        /// </summary>
        public AnchorPoint anchor { get; private set; }

        /// <summary>
        /// If the current ground collider implements <see cref="IMovablePlatform"/>,
        /// this property will cache that interface for convenient platform-relative
        /// motion handling.
        /// </summary>
        public IMovablePlatform movingAnchor { get; private set; }


        /// <summary>
        /// Transition into the grounded state using <paramref name="newAnchorPoint"/>
        /// as the contact anchor. This updates velocity (vertical component becomes 0),
        /// sets the moving anchor if available and invokes <see cref="PhysicsBody.OnLand"/>.
        /// </summary>
        /// <param name="newAnchorPoint">The Raycast/contact information representing the ground.</param>
        public void Land(AnchorPoint newAnchorPoint)
        {
            if (!HasOwner) return;
            bool wasntGrounded = value != Values.Grounded;
            bool objectChange = anchor.collider != newAnchorPoint.collider;

            if (!wasntGrounded && !objectChange) return;

            value = Values.Grounded;
            anchor = newAnchorPoint;
            Body.Velocity.y = 0;

            if (objectChange)
            {
                movingAnchor?.RemoveBody(Body);
                movingAnchor = newAnchorPoint.collider.GetComponent<IMovablePlatform>();
                movingAnchor?.AddBody(Body);
            }

            if (wasntGrounded)
            {

            }

            Body.OnLand(wasntGrounded, objectChange);
            //OnNavMesh = true;
        }
        /// <summary>
        /// Convenience overload that performs a ground check and Lands on the first
        /// valid detected surface.
        /// </summary>
        public void Land()
        {
            if (!HasOwner) return;
            if (!Check(out AnchorPoint groundHit)) return;
            Land(groundHit);
        }
        /// <summary>
        /// Transitions out of the grounded state into an airborne state specified by
        /// <paramref name="newState"/>. Clears anchor and moving anchor references
        /// and calls <see cref="PhysicsBody.OnUnLand"/>.
        /// </summary>
        /// <param name="newState">The airborne state to transition into. Must be >= Jumping.</param>
        public void UnLand(Values newState = Values.Falling)
        {
            if (!HasOwner) return;
            if (newState < Values.Jumping) return;
            value = newState;
            anchor = AnchorPoint.Null;
            if (movingAnchor != null)
            {
                movingAnchor?.RemoveBody(Body);
                movingAnchor = null;
            }
            Body.OnUnLand(newState);
            //OnNavMesh = false;
        }

        /// <summary>
        /// Checks if the character is grounded and outputs the ground hit information.
        /// </summary>
        /// <param name="groundHit">The anchor point of the ground hit.</param>
        /// <returns>True if grounded, false otherwise.</returns>
        /// <summary>
        /// Performs a sweep downwards to determine whether the body is currently
        /// grounded. Returns the detected anchor point (if any).
        /// </summary>
        /// <param name="groundHit">Outputs the AnchorPoint detected or AnchorPoint.Null if none found.</param>
        /// <param name="dontApply">When true, prevents certain post-processing side-effects in callers (unused here).</param>
        /// <returns>True when a standable surface was detected beneath the body.</returns>
        public bool Check(out AnchorPoint groundHit, bool dontApply = false)
        {
            bool result = Body.Sweep(Vector3.down * groundCheckBuffer, out RaycastHit raycast, groundCheckBuffer) && WithinSlopeAngle(raycast.normal);
            groundHit = AnchorPoint.Null;
            if (!dontApply) groundHit = raycast;
            return result;
        }
        /// <summary>
        /// Checks if the character is grounded and outputs the ground hit information.
        /// </summary>
        /// <param name="groundHit">The anchor point of the ground hit.</param>
        /// <returns>True if grounded, false otherwise.</returns>
        /// <summary>
        /// Performs a sweep downwards to determine whether the body is currently
        /// grounded and returns both an AnchorPoint and the raw RaycastHit.
        /// </summary>
        /// <param name="groundHit">Outputs the AnchorPoint detected or AnchorPoint.Null if none found.</param>
        /// <param name="raycast">Outputs the raw RaycastHit from the internal sweep.</param>
        /// <param name="dontApply">When true, prevents certain post-processing side-effects in callers (unused here).</param>
        /// <returns>True when a standable surface was detected beneath the body.</returns>
        public bool Check(out AnchorPoint groundHit, out RaycastHit raycast, bool dontApply = false)
        {
            bool result = Body.Sweep(Vector3.down * groundCheckBuffer, out raycast, groundCheckBuffer) && WithinSlopeAngle(raycast.normal);
            groundHit = AnchorPoint.Null;
            if (!dontApply) groundHit = raycast;
            return result;
        }

        /// <summary>
        /// Instantly snaps the character to the floor below, if any, and outputs the hit information.
        /// </summary>
        /// <param name="hit">The RaycastHit of the floor.</param>
        /// <returns>True if snapped to floor, false otherwise.</returns>
        /// <summary>
        /// Attempts an immediate snap to the floor by sweeping a long distance downwards
        /// and moving the body to the detected surface when present. Useful for initial
        /// positioning in Awake.
        /// </summary>
        /// <param name="hit">Outputs the RaycastHit that was used for the snap.</param>
        /// <returns>True if a floor was found and the body was moved, otherwise false.</returns>
        public bool InstantSnapToFloor(out RaycastHit hit)
        {
            if (Body.Sweep(Vector3.down * 1000, out hit, .5f))
            {
                Body.Position += Vector3.down * hit.distance;
                return true;
            }
            return false;
        }


        /// <summary>
        /// Determines if the given normal is within the allowed slope angle.
        /// </summary>
        /// <param name="inNormal">The normal to check.</param>
        /// <returns>True if within the slope angle, false otherwise.</returns>
        /// <summary>
        /// Returns true if the supplied normal corresponds to a slope that is less
        /// steep than <see cref="maxSlopeNormalAngle"/>.
        /// </summary>
        /// <param name="inNormal">Surface normal to evaluate.</param>
        /// <returns>True for standable slopes.</returns>
        public bool WithinSlopeAngle(Vector3 inNormal) => Vector3.Angle(Vector3.up, inNormal) < maxSlopeNormalAngle;



    }
}