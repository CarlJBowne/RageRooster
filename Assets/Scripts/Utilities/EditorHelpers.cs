using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public static class EditorHelpers
{
    public static void DrawEditableFlatCone(Vector3 origin, Vector3 forward, Vector3 up, SerializedProperty distance, SerializedProperty angle, Color color)
    {
        if (distance == null || angle == null) return;

        var so = distance.serializedObject ?? angle.serializedObject;
        if (so == null) return;
        so.Update();

        // Read current values
        float currentDistance = distance.floatValue;
        float currentHalfAngle = angle.floatValue;

        // Prepare forward on horizontal plane
        Vector3 f = forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.0001f)
            f = forward.normalized;
        f.Normalize();

        // Draw cone visuals (filled arc + wire + radius lines)
        float halfAngle = Mathf.Abs(currentHalfAngle);
        Vector3 startDir = Quaternion.AngleAxis(-halfAngle, up) * f;


        Handles.color = color;
        Handles.DrawWireArc(origin, up, startDir, halfAngle * 2f, currentDistance);
        Handles.DrawLine(origin, origin + (Quaternion.AngleAxis(-halfAngle, up) * f) * currentDistance);
        Handles.DrawLine(origin, origin + (Quaternion.AngleAxis(halfAngle, up) * f) * currentDistance);

        Handles.color = color.SetAlpha(.12f);
        Handles.DrawSolidArc(origin, up, startDir, halfAngle * 2f, currentDistance);

        // Interactive handles
        EditorGUI.BeginChangeCheck();

        // Distance slider handle
        Handles.color = color;
        Vector3 distHandlePos = origin + f * currentDistance;
        Vector3 newDistHandlePos = Handles.Slider(distHandlePos, f, HandleUtility.GetHandleSize(distHandlePos) * .15f, Handles.ConeHandleCap, 0f);
        float newDistance = currentDistance;
        {
            Vector3 delta = newDistHandlePos - origin;
            float projected = Vector3.Dot(delta, f);
            newDistance = Mathf.Max(0f, projected);
        }

        // Angle free-move handle
        float newHalfAngle = Mathf.Abs(currentHalfAngle);
        Vector3 rimDir = Quaternion.AngleAxis(halfAngle, up) * f;
        Vector3 angleHandlePos = origin + rimDir * currentDistance;
        Handles.color = color;
        Vector3 newAngleHandlePos = Handles.FreeMoveHandle(angleHandlePos, HandleUtility.GetHandleSize(angleHandlePos) * 0.08f, Vector3.zero, Handles.SphereHandleCap);
        {
            Vector3 dir = newAngleHandlePos - origin;
            dir.y = 0f;
            if (dir.sqrMagnitude >= 0.0001f)
            {
                dir.Normalize();
                float angleSigned = Vector3.SignedAngle(f, dir, up);
                newHalfAngle = Mathf.Abs(angleSigned);
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            distance.floatValue = Mathf.Max(0f, newDistance);
            angle.floatValue = Mathf.Clamp(newHalfAngle, 0f, 180f);
            so.ApplyModifiedProperties();
            UnityEditor.SceneView.RepaintAll();
        }
    }

    public static void DrawEditableFlatCone(Transform reference, SerializedProperty distance, SerializedProperty angle, Color color)
    {
        if (reference == null) return;
        DrawEditableFlatCone(reference.position, reference.forward, reference.up, distance, angle, color);
    }

}