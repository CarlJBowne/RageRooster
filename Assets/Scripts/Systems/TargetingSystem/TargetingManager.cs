using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EditorAttributes;
using Unity.VisualScripting;



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
    public static TargetingRange Aim;

    [System.Serializable]
    public class TargetingRange
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




    #region Instance Fields

    [SerializeField] private TargetingRange hipFireAim = new();
    [SerializeField] private TargetingRange aimingAim = new(); 

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
        private static TargetingManager This;
        private SerializedProperty p_hipFireAim;
        private SerializedProperty p_hipFireAim_Distance;
        private SerializedProperty p_hipFireAim_Angle;
        private SerializedProperty p_aimingAim;
        private SerializedProperty p_aimingAim_Distance;
        private SerializedProperty p_aimingAim_Angle;

        // UI state (editor instance)
        private bool showCones = false;

        private void OnEnable()
        {
            This = (TargetingManager)target;
            p_hipFireAim = serializedObject.FindProperty(nameof(hipFireAim));
            p_hipFireAim_Angle = p_hipFireAim?.FindPropertyRelative(nameof(TargetingRange.maxAngle));
            p_hipFireAim_Distance = p_hipFireAim?.FindPropertyRelative(nameof(TargetingRange.maxDistance));
            p_aimingAim = serializedObject.FindProperty(nameof(aimingAim));
            p_aimingAim_Angle = p_aimingAim?.FindPropertyRelative(nameof(TargetingRange.maxAngle));
            p_aimingAim_Distance = p_aimingAim?.FindPropertyRelative(nameof(TargetingRange.maxDistance));
            UnityEditor.SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDisable()
        {
            UnityEditor.SceneView.duringSceneGui -= DuringSceneGUI;
            This = null;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var so = serializedObject;
            so.Update();

            if (p_hipFireAim != null)
            {
                var hipField = new PropertyField(p_hipFireAim);
                hipField.SetEnabled(true);
                root.Add(hipField);
            }

            if (p_aimingAim != null)
            {
                var aimField = new UnityEditor.UIElements.PropertyField(p_aimingAim);
                aimField.SetEnabled(true);
                root.Add(aimField);
            }

            Button toggleButton = null;
            toggleButton = new Button(ConeToggle)
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

            void SetButtonStyle(Button b)
            {
                if (b == null) return;
                if (showCones)
                {
                    b.style.backgroundColor = new StyleColor(new Color(0.1f, 0.45f, 0.1f));
                    b.style.color = new StyleColor(Color.white);
                }
                else
                {
                    b.style.backgroundColor = new StyleColor(new Color(0.32f, 0.32f, 0.32f));
                    b.style.color = new StyleColor(Color.white);
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
            if (!showCones || This == null) return;


            serializedObject.Update();

            Transform hipFireTransform = p_hipFireAim.FindPropertyRelative(nameof(TargetingRange.front)).objectReferenceValue as Transform;
            Transform aimTransform = p_aimingAim.FindPropertyRelative(nameof(TargetingRange.front)).objectReferenceValue as Transform;

            DrawEditableFlatCone(hipFireTransform, p_hipFireAim_Angle, p_hipFireAim_Distance, Color.darkOrchid);
            DrawEditableFlatCone(aimTransform, p_aimingAim_Angle, p_aimingAim_Distance, Color.cyan);

            // Interactive handles
            EditorGUI.BeginChangeCheck();

        }


        /*
         PSEUDOCODE / DETAILED PLAN:
         
         - Guard: if either `distance` or `angle` SerializedProperty is null, return early.
         - Ensure the SerializedObject is up-to-date by calling `serializedObject.Update()`.
         - Read current float values from `distance.floatValue` and `angle.floatValue`.
         - Prepare a horizontal forward vector:
           - Project `forward` onto the XZ plane (set y = 0).
           - If projected magnitude is nearly zero, fallback to normalized `forward`.
           - Normalize the forward vector.
         - Draw the flat cone visualization (filled arc, wire arc, and radius lines) using the current values.
         - Begin an EditorGUI change check.
         - Distance handle:
           - Position a slider at `origin + forward * currentDistance`.
           - When moved, compute the projection of the slider position onto `forward`.
           - Clamp to >= 0.
         - Angle handle:
           - Compute rim direction using `Quaternion.AngleAxis(halfAngle, up) * forward`.
           - Place a free-move handle at `origin + rimDir * currentDistance`.
           - When moved, compute the direction from origin to handle (flatten Y), normalize, compute SignedAngle between forward and this dir around `up`, take abs -> new half-angle.
         - If any handle changed:
           - Write modified values directly to `distance.floatValue` and `angle.floatValue` (with clamping).
           - Call `serializedObject.ApplyModifiedProperties()` to persist and enable undo.
           - Repaint SceneView.
        */
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
            if (reference == null) reference = This.transform;
            DrawEditableFlatCone(reference.position, reference.forward, reference.up, distance, angle, color);
        }



        // Retain default inspector IMGUI as fallback
        public override void OnInspectorGUI() => DrawDefaultInspector();
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