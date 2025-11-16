using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EditorAttributes;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

public class TargetingManager : MonoBehaviour
{
    // Static runtime state
    public static TargetingManager Instance { get; private set; }
    public static List<Target> ALLTARGETS = new();
    public static Target CurrentTarget { get; private set; }
    public static TargetingAim Aim;

    [System.Serializable]
    public class TargetingAim
    {
        [Tooltip("Transform to source the position and forward direction from for comparisons")]
        public Transform front;
        [Tooltip("Maximum distance to target")]
        public float maxDistance = 10f;
        [Tooltip("Maximum angle difference from front to target")]
        public float maxAngle = 35f;
        [Tooltip("Determines the weighting between distance from the player and angle from the targeting retical. 1 = All Distance, 0 = All Angle. Allways keep between 0 and 1"), Range(.01f, .99f)]
        public float distanceAngleWeighting = .5f;
    }



    // Derived static properties

    #region Instance Fields

    // make fields serialized so they show up in inspector and the editor code can find them
    [SerializeField] private TargetingAim hipFireAim = new();
    [SerializeField] private TargetingAim aimingAim = new(); 

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

    public static void ToggleAimingDownSights(bool value) => Aim = value ? Instance.aimingAim : Instance.hipFireAim;



    // Unity lifecycle
    private void Awake()
    {
        Instance = this;
        ToggleAimingDownSights(false);
    }


    private void FixedUpdate()
    {
        if(Aim == null) return;
        Target NewTarget = GetBestTarget();
        if(NewTarget != CurrentTarget) CurrentTarget = NewTarget;
    }

    // Main selection logic
    public static Target GetBestTarget()
    {
        int chosenIndex = -1;
        float closestScore = 5f; //Max possible score is 2 (1 distance + 1 angle)

        for (int i = 0; i < ALLTARGETS.Count; i++)
        {
            if (ALLTARGETS[i] == null || ALLTARGETS[i].enabled == false) continue;

            float distance = Vector3.Distance(Aim.front.position, ALLTARGETS[i].transform.position);
            float angle = Vector3.Angle(Aim.front.forward, ALLTARGETS[i].transform.position - Aim.front.position);

            ALLTARGETS[i].WithinRange = distance > Aim.maxDistance || angle > Aim.maxAngle;
            if (!ALLTARGETS[i].WithinRange) continue;

            float distanceScore = distance / Aim.maxDistance;
            float angleScore = angle / Aim.maxAngle;
            float finalScore = (distanceScore * Aim.distanceAngleWeighting) + (angleScore * (1f - Aim.distanceAngleWeighting));

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
        /*
         DETAILED PSEUDOCODE / PLAN:

         1) State & SerializedProperties
            - Cache TargetingManager instance 'tm' and two SerializedProperty references:
              p_hipFireAim -> serializedObject.FindProperty("hipFireAim")
              p_aimingAim  -> serializedObject.FindProperty("aimingAim")
            - Keep a local bool 'showCones' to track toggle state for SceneView drawing.

         2) OnEnable / OnDisable
            - Assign 'tm' from target.
            - Find the two serialized properties.
            - Register and unregister a callback to UnityEditor.SceneView.duringSceneGui.

         3) CreateInspectorGUI
            - Create a root VisualElement.
            - Add PropertyField for hip and aim properties (they expose nested fields automatically).
            - Add a toggle button labeled "Show Target Cones".
              - Toggle flips 'showCones', updates button visuals and repaints SceneView.
            - Bind the root to serializedObject so editing supports undo/redo.
            - Return root.

         4) SceneView drawing entrypoint
            - When SceneView requests GUI, call a single function DrawConesAndHandles().
            - If showCones is false or tm is null -> return early.

         5) DrawConesAndHandles responsibilities (combined cone + handles)
            - Choose an origin/forward: prefer aiming front's Transform (p_aimingAim.front),
              else hip front, else tm.transform.
            - Project forward to horizontal plane (y=0), normalize, determine up = Vector3.up.
            - Read floats from serialized properties (hip/aim distances and angles) using helper.
              Use fallback defaults if properties missing.
            - Draw both cones on the ground plane (filled sector + wire arc + radius lines).
            - Draw interactive handles for each cone's distance and angle immediately after drawing.
              - Use Handles.Slider for distance: slide along forward axis.
              - Use Handles.FreeMoveHandle on the rim for angle: compute new angle from handle position.
            - Wrap handle interactions in EditorGUI.BeginChangeCheck / EndChangeCheck.
            - If changed, write new float values back to the appropriate SerializedProperty fields
              and apply modified properties (supports Undo).
            - Always call SceneView.RepaintAll when toggling or after applying props so editor updates.

         6) Helpers
            - ReadFloatFromAimProperty(SerializedProperty aimProp, string name, float fallback)
            - WriteFloatToAimProperty(SerializedProperty aimProp, string name, float value)
            - DrawFlatCone(...) draws filled arc and wire arcs/lines.
            - EditDistanceHandle(...) returns new distance float.
            - EditAngleHandle(...) returns new half-angle float.

         This simplified editor consolidates drawing and interactive editing in one place,
         keeps UIElements inspector minimal and binds to serializedObject for undo support.
        */

        // Cached runtime/serialized references
        private TargetingManager tm;
        private SerializedProperty p_hipFireAim;
        private SerializedProperty p_aimingAim;

        // UI state (editor instance)
        private bool showCones = false;

        private void OnEnable()
        {
            tm = (TargetingManager)target;
            p_hipFireAim = serializedObject.FindProperty("hipFireAim");
            p_aimingAim = serializedObject.FindProperty("aimingAim");
            UnityEditor.SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDisable()
        {
            UnityEditor.SceneView.duringSceneGui -= DuringSceneGUI;
        }

        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI()
        {
            var root = new UnityEngine.UIElements.VisualElement();
            var so = serializedObject;
            so.Update();

            if (p_hipFireAim != null)
            {
                var hipField = new UnityEditor.UIElements.PropertyField(p_hipFireAim);
                hipField.SetEnabled(true);
                root.Add(hipField);
            }

            if (p_aimingAim != null)
            {
                var aimField = new UnityEditor.UIElements.PropertyField(p_aimingAim);
                aimField.SetEnabled(true);
                root.Add(aimField);
            }

            UnityEngine.UIElements.Button toggleButton = null;
            toggleButton = new UnityEngine.UIElements.Button(ConeToggle)
            {text = "Show Target Cones"};

            SetButtonStyle(toggleButton);
            root.Add(toggleButton);

            root.Bind(so);
            return root;

            void ConeToggle()
            {
                showCones = !showCones;
                SetButtonStyle(toggleButton);
                UnityEditor.SceneView.RepaintAll();
            }

            void SetButtonStyle(UnityEngine.UIElements.Button b)
            {
                if (b == null) return;
                if (showCones)
                {
                    b.style.backgroundColor = new UnityEngine.UIElements.StyleColor(new Color(0.1f, 0.45f, 0.1f));
                    b.style.color = new UnityEngine.UIElements.StyleColor(Color.white);
                }
                else
                {
                    b.style.backgroundColor = new UnityEngine.UIElements.StyleColor(new Color(0.32f, 0.32f, 0.32f));
                    b.style.color = new UnityEngine.UIElements.StyleColor(Color.white);
                }
            }
        }

        // SceneView hook
        private void DuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            DrawConesAndHandles();
        }

        // Combined drawing + handle editing for both hip and aim cones
        private void DrawConesAndHandles()
        {
            if (!showCones || tm == null) return;

            // Choose transform from aiming front -> hip front -> component transform
            Transform aimTransform = null;

            serializedObject.Update();
            if (p_aimingAim != null)
            {
                var frontProp = p_aimingAim.FindPropertyRelative("front");
                if (frontProp != null && frontProp.objectReferenceValue != null)
                    aimTransform = frontProp.objectReferenceValue as Transform;
            }

            if (aimTransform == null && p_hipFireAim != null)
            {
                var frontProp = p_hipFireAim.FindPropertyRelative("front");
                if (frontProp != null && frontProp.objectReferenceValue != null)
                    aimTransform = frontProp.objectReferenceValue as Transform;
            }

            if (aimTransform == null) aimTransform = tm.transform;
            if (aimTransform == null) return;

            Vector3 origin = aimTransform.position;
            Vector3 forward = aimTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = aimTransform.TransformDirection(Vector3.forward);
            forward = forward.normalized;
            Vector3 up = Vector3.up;

            // Read values
            float hipDistance = ReadFloatFromAimProperty(p_hipFireAim, "maxDistance", 10f);
            float aimDistance = ReadFloatFromAimProperty(p_aimingAim, "maxDistance", 10f);
            float hipAngle = ReadFloatFromAimProperty(p_hipFireAim, "maxAngle", 35f);
            float aimAngle = ReadFloatFromAimProperty(p_aimingAim, "maxAngle", 35f);

            // Draw cones
            DrawFlatCone(origin, forward, up, hipDistance, hipAngle, new Color(1f, 0.2f, 0.2f, 0.12f), Color.red);
            DrawFlatCone(origin, forward, up, aimDistance, aimAngle, new Color(0.2f, 1f, 0.2f, 0.12f), Color.green);

            // Interactive handles
            EditorGUI.BeginChangeCheck();

            float newHipDistance = EditDistanceHandle(origin, forward, hipDistance, Color.red);
            float newHipAngle = EditAngleHandle(origin, forward, up, hipAngle, hipDistance, Color.red);

            float newAimDistance = EditDistanceHandle(origin, forward, aimDistance, Color.green);
            float newAimAngle = EditAngleHandle(origin, forward, up, aimAngle, aimDistance, Color.green);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.Update();
                WriteFloatToAimProperty(p_hipFireAim, "maxDistance", Mathf.Max(0f, newHipDistance));
                WriteFloatToAimProperty(p_hipFireAim, "maxAngle", Mathf.Clamp(newHipAngle, 0f, 180f));
                WriteFloatToAimProperty(p_aimingAim, "maxDistance", Mathf.Max(0f, newAimDistance));
                WriteFloatToAimProperty(p_aimingAim, "maxAngle", Mathf.Clamp(newAimAngle, 0f, 180f));
                serializedObject.ApplyModifiedProperties();
                UnityEditor.SceneView.RepaintAll();
            }
        }

        // Helpers
        private float ReadFloatFromAimProperty(SerializedProperty aimProp, string relativeName, float fallback)
        {
            if (aimProp == null) return fallback;
            var rel = aimProp.FindPropertyRelative(relativeName);
            return rel != null ? rel.floatValue : fallback;
        }

        private void WriteFloatToAimProperty(SerializedProperty aimProp, string relativeName, float value)
        {
            if (aimProp == null) return;
            var rel = aimProp.FindPropertyRelative(relativeName);
            if (rel != null) rel.floatValue = value;
        }

        private void DrawFlatCone(Vector3 origin, Vector3 forward, Vector3 up, float distance, float halfAngleDeg, Color fillColor, Color wireColor)
        {
            float halfAngle = Mathf.Abs(halfAngleDeg);
            Vector3 startDir = Quaternion.AngleAxis(-halfAngle, up) * forward;

            Handles.color = fillColor;
            Handles.DrawSolidArc(origin, up, startDir, halfAngle * 2f, distance);

            Handles.color = wireColor;
            Handles.DrawWireArc(origin, up, startDir, halfAngle * 2f, distance);
            Handles.DrawLine(origin, origin + (Quaternion.AngleAxis(-halfAngle, up) * forward) * distance);
            Handles.DrawLine(origin, origin + (Quaternion.AngleAxis(halfAngle, up) * forward) * distance);
        }

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

        private float EditAngleHandle(Vector3 origin, Vector3 forward, Vector3 up, float currentHalfAngle, float distance, Color color)
        {
            float halfAngle = Mathf.Abs(currentHalfAngle);
            Vector3 rimDir = Quaternion.AngleAxis(halfAngle, up) * forward;
            Vector3 handlePos = origin + rimDir * distance;

            Handles.color = color;
            EditorGUI.BeginChangeCheck();
            var fmh_360_64_638988340468543629 = Quaternion.identity; Vector3 newPos = Handles.FreeMoveHandle(handlePos, HandleUtility.GetHandleSize(handlePos) * 0.08f, Vector3.zero, Handles.SphereHandleCap);
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

        // Retain default inspector IMGUI as fallback
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
#endif
}

public class Target : MonoBehaviour
{

    public float GetDistance() => Vector3.Distance(TargetingManager.Aim.front.position, transform.position);

    public float GetAngle() => Vector3.Angle(TargetingManager.Aim.front.forward, transform.position - TargetingManager.Aim.front.position);

    protected virtual void OnEnable() => TargetingManager.AddActiveTarget(this);
    protected virtual void OnDisable() => TargetingManager.RemoveActiveTarget(this);

    public bool WithinRange { internal set; get; }

    public bool IsTargeted => TargetingManager.CurrentTarget == this;







}