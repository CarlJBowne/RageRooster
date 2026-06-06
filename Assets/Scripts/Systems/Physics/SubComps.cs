using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RageRooster.Physics
{

    /// <summary>
    /// <see cref="PhysicsBody"/> Sub-component that tracks velocity for a PhysicsBody in both local (forward/side/up) and global (x/y/z) coordinate spaces. Assigning to one representation will update the other representations automatically.
    /// </summary>
    public class Velocity
    {
        #region Relations
        /// <summary>
        /// The owning PhysicsBody instance. This will be set by calling <see cref="Init"/>.
        /// </summary>
        public PhysicsBody body { get; private set; }

        /// <summary>
        /// Whether this instance has been initialized and has an owner set.
        /// </summary>
        public bool HasOwner => body != null;

        /// <summary>
        /// Initializes the Velocity instance with its owning <see cref="PhysicsBody"/>.
        /// </summary>
        /// <param name="owner">The physics body that owns this velocity container.</param>
        public void Init(PhysicsBody owner) => body = owner;

        /// <summary>
        /// Convenience accessor for the owner's transform.
        /// </summary>
        public Transform transform => body.transform;
        #endregion

        /// <summary>
        /// Forward Velocity
        /// <br/> Setting this will rebuild the x and z values to match the new forward velocity.
        /// </summary>
        public float f
        {
            get => fValue;
            set
            {
                fValue = value;
                if (!allowBackwards && value < 0) fValue = 0;
                Vector3 global = transform.TransformVector(Local);
                zValue = global.z;
                xValue = global.x;
            }
        }
        float fValue;
        /// <summary>
        /// Upward Velocity (Identical to y)
        /// </summary>
        public float u { get => yValue; set => yValue = value; }
        /// <summary>
        /// Sideways Velocity
        /// <br/> Setting this will rebuild the x and z values to match the new sideways velocity.
        /// </summary>
        public float s
        {
            get => sValue;
            set
            {
                sValue = value;
                Vector3 global = transform.TransformVector(Local);
                zValue = global.z;
                xValue = global.x;
            }
        }
        float sValue;

        /// <summary>
        /// Velocity on the X global direction
        /// <br/> Setting this will rebuild the f and s values to match the new x velocity.
        /// </summary>
        public float x
        {
            get => xValue;
            set
            {
                xValue = value;
                Vector3 local = transform.InverseTransformVector(Global);
                fValue = local.z;
                sValue = local.x;
            }
        }
        float xValue;
        /// <summary>
        /// Velocity on the Y global direction (Identical to u)
        /// </summary>
        public float y
        {
            get => yValue; set => yValue = value;
        }
        float yValue;
        /// <summary>
        /// Velocity on the Z global direction
        /// <br/> Setting this will rebuild the f and s values to match the new z velocity.
        /// </summary>
        public float z
        {
            get => zValue;
            set
            {
                zValue = value;
                Vector3 local = transform.InverseTransformVector(Global);
                fValue = local.z;
                sValue = local.x;
            }
        }
        float zValue;



        public Vector3 Global
        {
            get => new(xValue, yValue, zValue);
            set
            {
                xValue = value.x;
                yValue = value.y;
                zValue = value.z;

                Vector3 local = transform.InverseTransformVector(Global);
                fValue = local.z;
                sValue = local.x;
            }
        }
        public Vector3 Local
        {
            get => new(sValue, yValue, fValue);
            set
            {
                sValue = value.x;
                yValue = value.y;
                fValue = value.z;

                Vector3 global = transform.TransformVector(Local);
                zValue = global.z;
                xValue = global.x;
            }
        }

        /// <summary>
        /// Rotational velocity around the vertical axis (Y). Positive values
        /// represent clockwise rotation when viewed from above.
        /// </summary>
        public float r
        {
            get => rValue;
            set => rValue = value;
        }
        float rValue;
        /// <summary>
        /// How much Local Velocity is carried over upon rotation. 0-1
        /// </summary>
        public float cL
        {
            get => cLValue;
            set
            {
                cLValue = Mathf.Clamp01(value);
                if (cLValue + cGValue > 1) cGValue = 1 - cLValue;
            }
        }
        float cLValue = 1f;
        /// <summary>
        /// How much Global Velocity is carried over upon rotation. 0-1
        /// </summary>
        public float cG
        {
            get => cGValue;
            set
            {
                cLValue = Mathf.Clamp01(value);
                if (cLValue + cGValue > 1) cGValue = 1 - cLValue;
            }
        }
        float cGValue;

        public bool allowBackwards = true;

        /// <summary>
        /// Call this after the transform or direction has been rotated. This method
        /// reconciles local/global velocity components according to the configured
        /// carry-over parameters (<see cref="cL"/> and <see cref="cG"/>).
        /// </summary>
        public void CallThisPostRotation()
        {
            if (cLValue == 0 && cGValue == 0) { xValue = 0; zValue = 0; fValue = 0; sValue = 0; return; }

            Vector3 adjustedGlobalValues = transform.InverseTransformVector(Global);

            float fFinal = (fValue * cLValue) + (adjustedGlobalValues.z * cGValue),
                  sFinal = (sValue * cLValue) + (adjustedGlobalValues.x * cGValue);

            Vector3 finalGlobalValues = transform.TransformVector(new(sFinal, 0, fFinal));

            fValue = fFinal;
            sValue = sFinal;
            xValue = finalGlobalValues.x;
            zValue = finalGlobalValues.z;
        }

        /// <summary>
        /// Zeros velocity components selectively.
        /// </summary>
        /// <param name="horizontal">Zero horizontal components (f and s / x and z).</param>
        /// <param name="vertical">Zero vertical component (y).</param>
        /// <param name="rotational">Zero rotational component (r).</param>
        public void ZeroOut(bool horizontal = true, bool vertical = true, bool rotational = true)
        {
            if (horizontal)
            {
                fValue = 0;
                sValue = 0;
                xValue = 0;
                zValue = 0;
            }
            if (vertical) yValue = 0;
            if (rotational) rValue = 0;
        }

        /// <summary>
        /// The current horizontal Magnitude of the current velocity.
        /// </summary>
        public float magnitudeH =>
            sValue != 0 ? MathF.Sqrt((fValue * fValue) + (sValue * sValue))
            : fValue;
        /// <summary>
        /// The current horizontal Squared Magnitude of the current velocity.
        /// </summary>
        public float sqrMagnitudeH =>
            sValue != 0 ? (fValue * fValue) + (sValue * sValue)
            : fValue * fValue;
        /// <summary>
        /// The current Magnitude of the current velocity.
        /// </summary>
        public float magnitude =>
            fValue != 0 && sValue != 0 && yValue != 0 ? Mathf.Sqrt((fValue * fValue) + (sValue * sValue) + (yValue * yValue)) //All 3 NonZero
                  : fValue != 0 && sValue == 0 && yValue == 0 ? Mathf.Sqrt(fValue * fValue) //Only F
                  : fValue == 0 && sValue == 0 && yValue != 0 ? Mathf.Sqrt(yValue * yValue) //Only Y
                  : fValue == 0 && sValue != 0 && yValue == 0 ? Mathf.Sqrt(sValue * sValue) //Only S
                  : fValue != 0 && sValue == 0 && yValue != 0 ? Mathf.Sqrt((fValue * fValue) + (yValue * yValue)) // F+Y
                  : fValue != 0 && sValue != 0 && yValue == 0 ? Mathf.Sqrt((fValue * fValue) + (sValue * sValue)) // F+S
                  : fValue == 0 && sValue != 0 && yValue != 0 ? Mathf.Sqrt((sValue * sValue) + (yValue * yValue)) // S+Y
            : 0; //All 3 Zero

        /// <summary>
        /// The current Squared Magnitude of the current velocity.
        /// </summary>
        public float sqrMagnitude =>
            fValue != 0 && sValue != 0 && yValue != 0 ? (fValue * fValue) + (sValue * sValue) + (yValue * yValue) //All 3 NonZero
                  : fValue != 0 && sValue == 0 && yValue == 0 ? fValue * fValue //Only F
                  : fValue == 0 && sValue == 0 && yValue != 0 ? yValue * yValue //Only Y
                  : fValue == 0 && sValue != 0 && yValue == 0 ? sValue * sValue //Only S
                  : fValue != 0 && sValue == 0 && yValue != 0 ? (fValue * fValue) + (yValue * yValue) // F+Y
                  : fValue != 0 && sValue != 0 && yValue == 0 ? (fValue * fValue) + (sValue * sValue) // F+S
                  : fValue == 0 && sValue != 0 && yValue != 0 ? (sValue * sValue) + (yValue * yValue) // S+Y
            : 0; //All 3 Zero
    }

    /// <summary>
    /// <see cref="PhysicsBody"/> Sub-component that tracks the facing direction for a PhysicsBody. The Direction is used when converting between local and global velocities and for rotation helper functions (quick turns, limited turns, etc.).
    /// </summary>
    public class Direction
    {
        #region Relations
        /// <summary>
        /// The owning PhysicsBody instance. Set by calling <see cref="Init"/>.
        /// </summary>
        public PhysicsBody body { get; private set; }

        /// <summary>
        /// Whether this Direction has been initialized with an owner.
        /// </summary>
        public bool HasOwner => body != null;

        /// <summary>
        /// Initializes the Direction with its owning <see cref="PhysicsBody"/>.
        /// </summary>
        /// <param name="owner">The physics body that owns this direction instance.</param>
        public void Init(PhysicsBody owner) => body = owner;

        /// <summary>
        /// Convenience accessor for the owner's transform.
        /// </summary>
        public Transform transform => body.transform;
        #endregion

        /// <summary>
        /// The currently cached forward vector used by the physics body.
        /// </summary>
        public Vector3 value { get; private set; }
        public static implicit operator Vector3(Direction This) => This.value;

        /// <summary>
        /// Smoothly rotates the current facing value toward <paramref name="target"/>
        /// using a maximum turn speed measured in degrees per second.
        /// </summary>
        /// <param name="target">Target forward vector in world space.</param>
        /// <param name="maxTurnDegrees">Maximum degrees per second to rotate.</param>
        public void Set(Vector3 target, float maxTurnDegrees)
        {
            if (target == Vector3.zero) return;
            Vector3 res = Vector3.RotateTowards(value, target.normalized, maxTurnDegrees * Mathf.PI, 1);
            Set(res);
        }
        /// <summary>
        /// Immediately sets the facing direction to <paramref name="target"/>
        /// and updates the underlying rotation quaternion on the owner's Rigidbody.
        /// </summary>
        /// <param name="target">Target forward vector in world space.</param>
        public void Set(Vector3 target)
        {
            if (value == target || target == Vector3.zero) return;
            value = target;
            RotationQ = Quaternion.LookRotation(target, Vector3.up);
        }

        /// <summary>
        /// Gets or sets the owner's rigidbody rotation as a Quaternion. Setting this
        /// property will call <see cref="Velocity.CallThisPostRotation"/> to keep
        /// the velocity representations consistent.
        /// </summary>
        public Quaternion RotationQ
        {
            get => body.RB.rotation;
            set
            {
                body.RB.rotation = value;
                body.velocity.CallThisPostRotation();
            }
        }
        /// <summary>
        /// Gets or sets the owner's transform.eulerAngles. Setting triggers <see cref="Velocity.CallThisPostRotation"/>
        /// </summary>
        public Vector3 Rotation
        {
            get => transform.eulerAngles;
            set
            {
                transform.eulerAngles = value;
                body.velocity.CallThisPostRotation();
            }
        }
        /// <summary>
        /// Gets or sets the owner's transform.eulerAngles.y. Setting triggers <see cref="Velocity.CallThisPostRotation"/>
        /// </summary>
        public float RotationY
        {
            get => Rotation.y;
            set
            {
                Vector3 prev = Rotation;
                prev.y = value;
                Rotation = prev;
            }
        }

        /// <summary>
        /// Performs a smooth quick-turn toward <paramref name="target"/> over the provided duration (in seconds). This method runs a coroutine and adjusts the facing vector incrementally each FixedUpdate.
        /// </summary>
        /// <param name="target">Target forward vector (XZ only).</param>
        /// <param name="lengthSeconds">Time duration to complete the quick turn.</param>
        public void QuickTurnTime(Vector3 target, float lengthSeconds)
        {
            target = target.XZ(); //Ensure no weird rotations

            if (lengthSeconds <= 0f)
            {
                value = target;
                return;
            }

            Coroutine.Begin(ref QuickTurnRoutine, Enum(), body, true);
            IEnumerator Enum()
            {
                float deltaRad = Vector3.Angle(value, target) * Mathf.Deg2Rad;
                float rateRadPerSec = deltaRad / lengthSeconds; // radians per second

                while (deltaRad > 0f)
                {
                    value = Vector3.RotateTowards(value, target, rateRadPerSec * Time.fixedDeltaTime, 0f);
                    yield return new WaitForFixedUpdate();
                    deltaRad -= rateRadPerSec * Time.fixedDeltaTime;
                }
                value = target;
            }
        }
        /// <summary>
        /// Performs a smooth quick-turn toward <paramref name="target"/> with the provided maximum delta. This method runs a coroutine and adjusts the facing vector incrementally each FixedUpdate.
        /// </summary>
        /// <param name="target">Target forward vector (XZ only).</param>
        /// <param name="maxDelta">The maximum delta the body is allowed to move during a frame.</param>
        public void QuickTurnLimited(Vector3 target, float maxDelta)
        {
            target = target.XZ(); //Ensure no weird rotations
            if (maxDelta <= 0f) return;

            Coroutine.Begin(ref QuickTurnRoutine, Enum(), body, true);
            IEnumerator Enum()
            {
                float fullDelta = Vector3.Angle(value, target) * Mathf.Deg2Rad;

                while (fullDelta > 0f)
                {
                    value = Vector3.RotateTowards(value, target, maxDelta * Time.fixedDeltaTime, 0f);
                    yield return null;
                    fullDelta -= maxDelta * Time.fixedDeltaTime;
                }

                value = target;
            }
        }
        private Coroutine QuickTurnRoutine;
    }

    /// <summary>
    /// <see cref="PhysicsBody"/> Sub-component that tracks whether the body is grounded and relevant information about the ground contact (normal, slope, collider, etc.). This component also provides helper methods for performing ground checks and transitioning between grounded and airborne states.
    /// </summary>
    public class GroundState
    {
        #region Relations
        /// <summary>
        /// The owning PhysicsBody instance. Set by calling <see cref="Init"/>.
        /// </summary>
        public PhysicsBody body { get; private set; }

        /// <summary>
        /// Whether this Direction has been initialized with an owner.
        /// </summary>
        public bool HasOwner => body != null;

        /// <summary>
        /// Initializes the Direction with its owning <see cref="PhysicsBody"/>.
        /// </summary>
        /// <param name="owner">The physics body that owns this direction instance.</param>
        public void Init(PhysicsBody owner) => body = owner;

        /// <summary>
        /// Convenience accessor for the owner's transform.
        /// </summary>
        public Transform transform => body.transform;
        #endregion

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

        /// <summary>
        /// Construct a new GroundState value container. Use <see cref="Init"/>
        /// to attach this state to its owning <see cref="PhysicsBody"/>.
        /// </summary>
        /// <param name="input">The initial ground value.</param>
        public GroundState(Values input) => value = input;

        /// <summary>
        /// The possible ground-related states for a body describing whether it is
        /// standing, airborne, in hangtime, etc.
        /// </summary>
        public enum Values
        {
            Grounded = 0,
            Jumping = 1,
            Decelerating = 2,
            Hangtime = 3,
            Falling = 4,
            TerminalVelocity = 5
        }
        /// <summary>
        /// The current ground state value.
        /// </summary>
        public Values value { get; private set; }

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
            body.velocity.y = 0;

            if (objectChange)
            {
                movingAnchor = newAnchorPoint.collider.GetComponent<IMovablePlatform>();
            }

            if (wasntGrounded)
            {

            }

            body.OnLand(wasntGrounded, objectChange);
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
            if (movingAnchor != null) movingAnchor = null;
            body.OnUnLand(newState);
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
            bool result = body.Sweep(Vector3.down * groundCheckBuffer, out RaycastHit raycast, groundCheckBuffer) && WithinSlopeAngle(raycast.normal);
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
            bool result = body.Sweep(Vector3.down * groundCheckBuffer, out raycast, groundCheckBuffer) && WithinSlopeAngle(raycast.normal);
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
            if (body.Sweep(Vector3.down * 1000, out hit, .5f))
            {
                body.Position += Vector3.down * hit.distance;
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


        #region Comparison

        public static implicit operator bool(GroundState This) => This.value == Values.Grounded;
        public static implicit operator Values(GroundState This) => This.value;
        public static implicit operator GroundState(Values input) => new(input);

        public static bool operator ==(GroundState This, GroundState other) => This.value == other.value;
        public static bool operator !=(GroundState This, GroundState other) => This.value != other.value;
        public static bool operator ==(GroundState This, Values other) => This.value == other;
        public static bool operator !=(GroundState This, Values other) => This.value != other;

        public const Values Grounded = Values.Grounded;
        public const Values Jumping = Values.Jumping;
        public const Values Decelerating = Values.Decelerating;
        public const Values Hangtime = Values.Hangtime;
        public const Values Falling = Values.Falling;
        public const Values TerminalVelocity = Values.TerminalVelocity;

        public override bool Equals(object obj) => obj is GroundState state && value == state.value && EqualityComparer<AnchorPoint>.Default.Equals(anchor, state.anchor);
        public override int GetHashCode() => HashCode.Combine(value, anchor);

        #endregion
    }

    /// <summary>
    /// A struct representing a contact point used for grounding and other physics interactions. Contains the contact point, normal, and collider information. Can be implicitly created from RaycastHit or ContactPoint data.
    /// </summary>
    public struct AnchorPoint
    {
        public Vector3 point;
        public Vector3 normal;
        public Collider collider;

        public AnchorPoint(Vector3 point, Vector3 normal, Collider collider)
        {
            this.point = point;
            this.normal = normal;
            this.collider = collider;
        }
        public AnchorPoint(RaycastHit hit)
        {
            point = hit.point;
            normal = hit.normal;
            collider = hit.collider;
        }
        public AnchorPoint(ContactPoint contact)
        {
            point = contact.point;
            normal = contact.normal;
            collider = contact.otherCollider;
        }

        public static implicit operator AnchorPoint(RaycastHit hit) => new(hit);
        public static implicit operator AnchorPoint(ContactPoint contact) => new(contact);
        public static implicit operator bool(AnchorPoint anchor) => anchor.point != Vector3.zero || anchor.normal != Vector3.zero || anchor.collider != null;
        public static implicit operator Vector3(AnchorPoint anchor) => anchor.normal;

        public static AnchorPoint Null => new()
        {
            point = Vector3.zero,
            normal = Vector3.up,
            collider = null
        };
    }
}