using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PhysicsPro
{

    /// <summary>
    /// Casts the Rigidbody in a direction to check for collision using SweepTest.
    /// </summary>
    /// <param name="rb">The Rigidbody in question.</param>
    /// <param name="direction">The direction the Rigidbody is going.</param>
    /// <param name="distance">The distance the Rigidbody is set to travel.</param>
    /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
    /// <param name="hit">The resulting Hit.</param>
    /// <returns>Whether anything was Hit.</returns>
    public static bool DirectionCast(this Rigidbody rb, Vector3 direction, float distance, float buffer, out RaycastHit hit)
    {
        rb.MovePosition(rb.position - direction * buffer);
        bool result = rb.SweepTest(direction.normalized, out hit, distance + buffer, QueryTriggerInteraction.Ignore);
        rb.MovePosition(rb.position + direction * buffer);
        hit.distance -= buffer;
        return result;
    }
    /// <summary>
    /// Casts the Rigidbody in a direction to check for collision using SweepTest. (Returns Multiple.)
    /// </summary>
    /// <param name="rb">The Rigidbody in question.</param>
    /// <param name="direction">The direction the Rigidbody is going.</param>
    /// <param name="distance">The distance the Rigidbody is set to travel.</param>
    /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
    /// <param name="hit">The resulting Hits.</param>
    /// <returns>Whether anything was Hit.</returns>
    public static bool DirectionCastAll(this Rigidbody rb, Vector3 direction, float distance, float buffer, out RaycastHit[] hit)
    {
        rb.MovePosition(rb.position - direction * buffer);
        hit = rb.SweepTestAll(direction.normalized, distance + buffer, QueryTriggerInteraction.Ignore);
        rb.MovePosition(rb.position + direction * buffer);
        hit[0].distance -= buffer;
        return hit.Length>0;
    }

    /* Reference
    static int maxBounces = 5;
    static float skinWidth = 0.015f;
    static float maxSlopeAngle = 55;

    public static Vector3 CollideAndSlide(this Rigidbody rb, Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit)
    {
        if (depth >= maxBounces) return Vector3.zero;

        if (rb.DirectionCast(vel.normalized, vel.magnitude, 0, out RaycastHit hit))
        {
            Vector3 snapToSurface = vel.normalized * (hit.distance);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            //if (snapToSurface.magnitude <= checkBuffer) snapToSurface = Vector3.zero;

            // normal ground / slope
            if (angle <= maxSlopeAngle)
            {
                if (gravityPass) return snapToSurface;
                leftover = leftover.ProjectAndScale(hit.normal);
            }
            else // wall or steep slope
            {
                float scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(velInit.x, 0, velInit.z).normalized
                    );

                leftover = true && !gravityPass
                    ? velInit.XZ().ProjectAndScale(hit.normal.XZ().normalized).normalized * scale
                    : leftover.ProjectAndScale(hit.normal) * scale;
            }
            return snapToSurface + rb.CollideAndSlide(leftover, pos + snapToSurface, depth + 1, gravityPass, velInit);
        }

        return vel;
    }

    public static Vector3 ProjectAndScale(Vector3 vec, Vector3 normal)
    {
        float mag = vec.magnitude;
        vec = Vector3.ProjectOnPlane(vec, normal).normalized;
        vec *= mag;
        return vec;
    }
    */



}