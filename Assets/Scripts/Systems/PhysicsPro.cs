using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PhysicsPro
{
    public static bool GroundCheck(this Rigidbody rb, float thickness = 0.005f)
    {
        rb.Move(rb.position + (0.5f * thickness * Vector3.up), rb.rotation);
        bool result = rb.SweepTest(Vector3.down, out _, thickness);
        rb.Move(rb.position + (0.5f * thickness * Vector3.down), rb.rotation);
        return result;
    }
    public static bool GroundCheck(this Rigidbody rb, out RaycastHit hitInfo, float thickness = 0.005f)
    {
        rb.Move(rb.position + (0.5f * thickness * Vector3.up), rb.rotation);
        bool result = rb.SweepTest(Vector3.down, out hitInfo, thickness);
        rb.Move(rb.position + (0.5f * thickness * Vector3.down), rb.rotation);
        return result;
    }
    public static bool GroundCheck(this Rigidbody rb, out RaycastHit[] groundInfo, float thickness = 0.005f)
    {
        rb.Move(rb.position + (0.5f * thickness * Vector3.up), rb.rotation);
        groundInfo = rb.SweepTestAll(Vector3.down, thickness);
        rb.Move(rb.position + (0.5f * thickness * Vector3.down), rb.rotation);
        return groundInfo.Length > 0;
    }

    static int maxBounces = 5;
    static float skinWidth = 0.015f;
    static float maxSlopeAngle = 55;

    public static Vector3 CollideAndSlide(this Rigidbody rb, Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit, bool isGrounded)
    {
        if (depth >= maxBounces) return Vector3.zero;

        float dist = vel.magnitude + skinWidth;

        if (rb.SweepTest(vel.normalized, out RaycastHit hit, dist))
        {
            Vector3 snapToSurface = vel.normalized * (hit.distance - skinWidth);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            if (snapToSurface.magnitude <= skinWidth)
            {
                snapToSurface = Vector3.zero;
            }

            // normal ground / slope
            if (angle <= maxSlopeAngle)
            {
                if (gravityPass) return snapToSurface;
                leftover = ProjectAndScale(leftover, hit.normal);
            }
            else // wall or steep slope
            {
                float scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(velInit.x, 0, velInit.z).normalized
                    );

                if(isGrounded && !gravityPass)
                {
                    leftover = ProjectAndScale(
                    new Vector3(velInit.x, 0, velInit.z),
                        new Vector3(hit.normal.x, 0, hit.normal.z).normalized
                        ).normalized * scale;
                }
                else
                {
                    leftover = ProjectAndScale(leftover, hit.normal) * scale;
                }
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




}