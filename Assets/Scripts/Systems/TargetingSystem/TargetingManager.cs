using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

public class TargetingManager : MonoBehaviour
{
    // Static runtime state
    private static List<Target> ALLTARGETS = new();
    private static bool isAimingDownSights = false;
    private static float MaxDistance;
    private static float MaxAngle;
    private static Transform Aim;

    public static TargetingManager Instance { get; private set; }

    // Derived static properties

    #region Instance Fields

    [Tooltip("Determines the weighting between distance from the player and angle from the targeting retical. 1 = All Distance, 0 = All Angle. Allways keep between .1 and .9")]
    [SerializeField] float distanceAngleWeighting;
    public static float DistanceAngleWeight;

    [Tooltip("Maximum distance to target while hip firing")]
    [SerializeField] float maxDistanceHipFire = 10f;
    [Tooltip("Maximum distance to target while aiming down sights")]
    [SerializeField] float maxDistanceAiming = 25f;
    [Tooltip("Maximum angle to target while hip firing")]
    [SerializeField] float maxAngleHipFire = 35f;
    [Tooltip("Maximum angle to target while aiming down sights")]
    [SerializeField] float maxAngleAiming = 10f;

    [Tooltip("The Transform to source the forward direction from when aiming down sights, rather than the Player's forward.")]
    [SerializeField] Transform aimingFocusPoint;

    #endregion

    // Public API: manage active targets
    public static void AddActiveTarget(Target target)
    {
        if (!ALLTARGETS.Contains(target))
            ALLTARGETS.Add(target);
    }

    public static void RemoveActiveTarget(Target target)
    {
        if (ALLTARGETS.Contains(target))
            ALLTARGETS.Remove(target);
    }

    public static void ToggleAimingDownSights(bool value)
    {
        if (isAimingDownSights == value) return;
        isAimingDownSights = value;
        MaxDistance = value ? Instance.maxDistanceAiming : Instance.maxDistanceHipFire;
        MaxAngle = value ? Instance.maxAngleAiming : Instance.maxAngleHipFire;
        Aim = value ? Instance.aimingFocusPoint : Player.Transform; //AddCenterTransform later.
    }



    // Unity lifecycle
    private void Awake()
    {
        Instance = this;
        DistanceAngleWeight = Mathf.Clamp(distanceAngleWeighting, .1f, .9f);
        ToggleAimingDownSights(false);
    }

    // Main selection logic
    public static Target GetBestTarget()
    {
        int chosenIndex = -1;
        float closestScore = 5f; //Max possible score is 2 (1 distance + 1 angle)

        for (int i = 0; i < ALLTARGETS.Count; i++)
        {
            float distance = Vector3.Distance(Aim.position, ALLTARGETS[i].transform.position);
            float angle = Vector3.Angle(Aim.forward, ALLTARGETS[i].transform.position - Aim.position);

            if (ALLTARGETS[i] == null || ALLTARGETS[i].enabled == false || distance > MaxDistance || angle > MaxAngle) continue;

            float distanceScore = distance / MaxDistance;
            float angleScore = angle / MaxAngle;
            float finalScore = (distanceScore * DistanceAngleWeight) + (angleScore * (1f - DistanceAngleWeight));

            if (finalScore < closestScore)
            {
                closestScore = finalScore;
                chosenIndex = i;
            }
        }
        return chosenIndex != -1 ? ALLTARGETS[chosenIndex] : null;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(TargetingManager))]
    public class Editor : UnityEditor.Editor
    {
        // Cached references
        private TargetingManager tm;
        private SerializedProperty p_distanceAngleWeighting;
        private SerializedProperty p_maxDistanceHipFire;
        private SerializedProperty p_maxDistanceAiming;
        private SerializedProperty p_maxAngleHipFire;
        private SerializedProperty p_maxAngleAiming;
        private SerializedProperty p_aimingFocusPoint;

        // UI state (per-editor instance)
        private bool showCones = false;

        private void OnEnable()
        {
            tm = (TargetingManager)target;
            p_distanceAngleWeighting = serializedObject.FindProperty("distanceAngleWeighting");
            p_maxDistanceHipFire = serializedObject.FindProperty("maxDistanceHipFire");
            p_maxDistanceAiming = serializedObject.FindProperty("maxDistanceAiming");
            p_maxAngleHipFire = serializedObject.FindProperty("maxAngleHipFire");
            p_maxAngleAiming = serializedObject.FindProperty("maxAngleAiming");
            p_aimingFocusPoint = serializedObject.FindProperty("aimingFocusPoint");

            // Ensure SceneView repaints when needed
            UnityEditor.SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDisable()
        {
            UnityEditor.SceneView.duringSceneGui -= DuringSceneGUI;
        }

        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI()
        {
            /*
            Pseudocode / Plan (detailed):
            - Create a root VisualElement to host the custom inspector UI.
            - Update the serializedObject so PropertyFields read current values.
            - Define the list of instance property names to expose and add a PropertyField for each:
              - For each property name: find the SerializedProperty, skip if null, create a PropertyField, enable it, and add it to the root.
            - Create a toggle button (`toggleButton`) that:
              - Toggles the `showCones` boolean when clicked.
              - Does NOT change its text based on state (text remains constant).
              - When `showCones` is true, darken the button by setting its background color to a darker color and set text color for readability.
              - When `showCones` is false, reset the background to transparent and set text color to default dark color.
              - Repaint the SceneView after toggling so visual changes update.
            - After creating the button, initialize its style based on the current `showCones` value so the inspector shows correct appearance at open time.
            - Bind the `serializedObject` to `root` so UI updates the object and supports undo/redo.
            - Return the root element.
            */

            // Root container for the inspector UI
            var root = new UnityEngine.UIElements.VisualElement();

            // Ensure serializedObject is current
            var so = serializedObject;
            so.Update();

            // List of instance configuration fields to expose
            var propNames = new[]
            {
                "distanceAngleWeighting",
                "maxDistanceHipFire",
                "maxDistanceAiming",
                "maxAngleHipFire",
                "maxAngleAiming",
                "aimingFocusPoint"
            };

            // Create and add a PropertyField for each available property
            foreach (var name in propNames)
            {
                var prop = so.FindProperty(name);
                if (prop == null) continue;

                var field = new UnityEditor.UIElements.PropertyField(prop);
                // Ensure fields are editable in the inspector
                field.SetEnabled(true);
                root.Add(field);
            }

            // Button to toggle showing cones + handles
            UnityEngine.UIElements.Button toggleButton = null;
            toggleButton = new UnityEngine.UIElements.Button(() =>
            {
                // Toggle state
                showCones = !showCones;

                // Update visual style instead of changing the name
                SetButtonColor(toggleButton);

                // Ensure SceneView repaints to reflect changes
                UnityEditor.SceneView.RepaintAll();
            })
            {
                // Keep a constant label per the request
                text = "Show Target Cones",
            };

            // Initialize button style to reflect current `showCones` value
            // Initialize button style to reflect current `showCones` value
            SetButtonColor(toggleButton);

            void SetButtonColor(Button toggleButton)
            {
                if (showCones)
                {
                    toggleButton.style.backgroundColor = new UnityEngine.UIElements.StyleColor(new Color(.3176471f, .3176471f, .3176471f, 1f));
                    toggleButton.style.color = new UnityEngine.UIElements.StyleColor(Color.white);
                }
                else
                {
                    toggleButton.style.backgroundColor = new UnityEngine.UIElements.StyleColor(Color.lightGray);
                    toggleButton.style.color = new UnityEngine.UIElements.StyleColor(Color.black);
                }
            }

            root.Add(toggleButton);

            // Bind so that the UI updates the serializedObject and supports undo
            root.Bind(so);

            return root;
        }

        // Hook for SceneView drawing
        private void DuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            OnSceneGUI();
        }

        // Scene drawing and interactive handles
        public void OnSceneGUI()
        {
            if (!showCones || tm == null) return;

            // Determine origin & forward
            Transform aimTransform = null;
            if (p_aimingFocusPoint != null && p_aimingFocusPoint.objectReferenceValue != null)
                aimTransform = p_aimingFocusPoint.objectReferenceValue as Transform;

            if (aimTransform == null)
            {
                // fallback to the component transform, or Player.Transform if available
                aimTransform = tm.aimingFocusPoint != null ? tm.aimingFocusPoint : tm.transform;
            }

            if (aimTransform == null) return;

            // Horizontal plane projection (Y-up)
            Vector3 origin = aimTransform.position;
            Vector3 forward = aimTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = aimTransform.TransformDirection(Vector3.forward);
            forward = forward.normalized;
            Vector3 up = Vector3.up;

            // Read serialized values for drawing (use defaults if properties missing)
            serializedObject.Update();
            float hipDistance = p_maxDistanceHipFire != null ? p_maxDistanceHipFire.floatValue : tm.maxDistanceHipFire;
            float aimDistance = p_maxDistanceAiming != null ? p_maxDistanceAiming.floatValue : tm.maxDistanceAiming;
            float hipAngle = p_maxAngleHipFire != null ? p_maxAngleHipFire.floatValue : tm.maxAngleHipFire;
            float aimAngle = p_maxAngleAiming != null ? p_maxAngleAiming.floatValue : tm.maxAngleAiming;
            serializedObject.ApplyModifiedProperties();

            // Draw Hip-fire cone (red)
            DrawFlatCone(origin, forward, up, hipDistance, hipAngle, new Color(1f, 0.2f, 0.2f, 0.12f), Color.red);

            // Draw Aiming cone (green)
            DrawFlatCone(origin, forward, up, aimDistance, aimAngle, new Color(0.2f, 1f, 0.2f, 0.12f), Color.green);

            // Interactive controls to change distance and angle for each mode (always shown when cones visible)
            EditorGUI.BeginChangeCheck();

            // HIP distance handle
            float newHipDistance = EditDistanceHandle(origin, forward, hipDistance, Color.red);
            // HIP angle handle
            float newHipAngle = EditAngleHandle(origin, forward, up, hipAngle, hipDistance, Color.red);

            // AIM distance handle
            float newAimDistance = EditDistanceHandle(origin, forward, aimDistance, Color.green);
            // AIM angle handle
            float newAimAngle = EditAngleHandle(origin, forward, up, aimAngle, aimDistance, Color.green);

            if (EditorGUI.EndChangeCheck())
            {
                // Apply back to serialized properties (supports undo)
                serializedObject.Update();
                if (p_maxDistanceHipFire != null) p_maxDistanceHipFire.floatValue = Mathf.Max(0f, newHipDistance);
                if (p_maxAngleHipFire != null) p_maxAngleHipFire.floatValue = Mathf.Clamp(newHipAngle, 0f, 180f);
                if (p_maxDistanceAiming != null) p_maxDistanceAiming.floatValue = Mathf.Max(0f, newAimDistance);
                if (p_maxAngleAiming != null) p_maxAngleAiming.floatValue = Mathf.Clamp(newAimAngle, 0f, 180f);
                serializedObject.ApplyModifiedProperties();
            }
        }

        // Draw a flat cone (filled + wire) on horizontal plane
        private void DrawFlatCone(Vector3 origin, Vector3 forward, Vector3 up, float distance, float halfAngleDeg, Color fillColor, Color wireColor)
        {
            // Ensure halfAngle is positive
            float halfAngle = Mathf.Abs(halfAngleDeg);

            // Compute start direction (rotated left by half angle)
            Vector3 startDir = Quaternion.AngleAxis(-halfAngle, up) * forward;

            // Filled sector
            Handles.color = fillColor;
            Handles.DrawSolidArc(origin, up, startDir, halfAngle * 2f, distance);

            // Wire arc + radius lines
            Handles.color = wireColor;
            Handles.DrawWireArc(origin, up, startDir, halfAngle * 2f, distance);
            Handles.DrawLine(origin, origin + (Quaternion.AngleAxis(-halfAngle, up) * forward) * distance);
            Handles.DrawLine(origin, origin + (Quaternion.AngleAxis(halfAngle, up) * forward) * distance);
        }

        // Distance handle: slider along forward direction
        private float EditDistanceHandle(Vector3 origin, Vector3 forward, float currentDistance, Color color)
        {
            Handles.color = color;
            Vector3 handlePos = origin + forward * currentDistance;
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.Slider(handlePos, forward, HandleUtility.GetHandleSize(handlePos) * 0.5f, Handles.ConeHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = newPos - origin;
                float projected = Vector3.Dot(delta, forward);
                return Mathf.Max(0f, projected);
            }
            return currentDistance;
        }

        // Angle handle: place a movable handle on the cone rim; dragging changes the angle
        private float EditAngleHandle(Vector3 origin, Vector3 forward, Vector3 up, float currentHalfAngle, float distance, Color color)
        {
            float halfAngle = Mathf.Abs(currentHalfAngle);

            // Handle placed on the outer rim at +halfAngle
            Vector3 rimDir = Quaternion.AngleAxis(halfAngle, up) * forward;
            Vector3 handlePos = origin + rimDir * distance;

            Handles.color = color;
            EditorGUI.BeginChangeCheck();
            var fmh_318_64_638985840470382382 = Quaternion.identity; Vector3 newPos = Handles.FreeMoveHandle(handlePos, HandleUtility.GetHandleSize(handlePos) * 0.1f, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 dir = newPos - origin;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) return halfAngle;
                dir.Normalize();
                float angle = Vector3.SignedAngle(forward, dir, up);
                return Mathf.Abs(angle);
            }

            return halfAngle;
        }

        // Keep default inspector GUI available in case user wants it (UIElements is used above)
        public override void OnInspectorGUI()
        {
            // Fall back to default inspector so values can still be edited via IMGUI if desired.
            DrawDefaultInspector();
        }
    }
#endif
}

public class Target : MonoBehaviour
{

}