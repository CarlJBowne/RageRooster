using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

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

    [SerializeField] private Color viewColor = Color.green;

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(TargetingRange))]
    public class PropertyDrawer : UnityEditor.PropertyDrawer
    {

        SerializedProperty p_transform;
        SerializedProperty p_distance;
        SerializedProperty p_angle;
        SerializedProperty p_distanceAngleWeighting;
        SerializedProperty p_viewColor;
        bool viewAndEdit = false;

        // Scene callback management
        private System.Action<UnityEditor.SceneView> sceneCallback;
        private bool registered = false;
        private SerializedObject registeredSerializedObject = null;

        // Plan / Pseudocode (detailed):
        // 1. When creating the UI for the property, cache child SerializedProperty references.
        // 2. Build a foldout and add PropertyFields and a "View/Edit" button.
        // 3. When the button toggles ON:
        //    - mark viewAndEdit = true
        //    - call RegisterScene with the current SerializedObject so we remember what we registered for
        //    - change button visuals and request SceneView repaint
        // 4. When the button toggles OFF or the inspector foldout is detached:
        //    - mark viewAndEdit = false
        //    - call UnregisterScene to remove our SceneView callback
        // 5. RegisterScene(serializedObject) will:
        //    - guard against duplicate registration
        //    - store the passed SerializedObject
        //    - create a sceneCallback that:
        //       - catches exceptions to avoid leaking GUI state
        //       - checks viewAndEdit and the registeredSerializedObject and cached properties for null
        //       - if invalid, calls UnregisterScene to stop callbacks
        //       - otherwise calls DrawEditableFlatCone()
        //    - subscribe sceneCallback to SceneView.duringSceneGui
        // 6. UnregisterScene will safely unsubscribe and clear stored state.
        // 7. DrawEditableFlatCone will be defensive: it will early-unregister if required serialized data is missing
        //    and wrap risky operations to avoid throwing during scene GUI callbacks.
        // 8. Additionally, register a DetachFromPanelEvent on the foldout to ensure UnregisterScene is called
        //    when the inspector UI is closed, selection changes, or domain reloads.
        // This prevents null refs and "GUI clip" errors caused by leftover scene callbacks after the property
        // or its serialized object has been destroyed or changed.

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {

            // Cache child properties
            p_transform = property.FindPropertyRelative(nameof(TargetingRange.front));
            p_distance = property.FindPropertyRelative(nameof(TargetingRange.maxDistance));
            p_angle = property.FindPropertyRelative(nameof(TargetingRange.maxAngle));
            p_distanceAngleWeighting = property.FindPropertyRelative(nameof(TargetingRange.distanceAngleWeighting));
            p_viewColor = property.FindPropertyRelative("viewColor");

            Foldout foldout = new();
            foldout.text = property.displayName;
            foldout.value = false;

            PropertyField transformField = new(p_transform);
            foldout.Add(transformField);
            PropertyField distanceField = new(p_distance);
            foldout.Add(distanceField);
            PropertyField angleField = new(p_angle);
            foldout.Add(angleField);
            PropertyField weightingField = new(p_distanceAngleWeighting);
            foldout.Add(weightingField);

            PropertyField colorField = new(p_viewColor);
            foldout.Add(colorField);

            var foldoutLabel = foldout.Q<Label>();

            Button viewEditButton = null;
            viewEditButton = new Button(() => ToggleViewEdit(viewEditButton))
            { text = "View/Edit" };
            viewEditButton.style.width = 80f;
            viewEditButton.style.alignSelf = Align.FlexEnd;
            foldoutLabel.Add(viewEditButton);

            void ToggleViewEdit(Button b)
            {
                viewAndEdit = !viewAndEdit;
                // Update visuals slightly to indicate state
                if (viewAndEdit)
                {
                    b.style.backgroundColor = new StyleColor(new Color(0.1f, 0.45f, 0.1f));
                    b.style.color = new StyleColor(Color.white);
                    // Register the scene callback bound to this serialized object instance
                    RegisterScene(property.serializedObject);
                }
                else
                {
                    b.style.backgroundColor = new StyleColor(new Color(0.32f, 0.32f, 0.32f));
                    b.style.color = new StyleColor(Color.white);
                    UnregisterScene();
                }

                UnityEditor.SceneView.RepaintAll();
            }

            // Ensure we unregister when the UI element is detached (selection changes, inspector closed, domain reload)
            foldout.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                // Turn off the view/edit flag and unregister the scene callback to avoid stale callbacks
                viewAndEdit = false;
                UnregisterScene();
                UnityEditor.SceneView.RepaintAll();
            });

            foldout.Bind(property.serializedObject);
            return foldout;
        }

        private void RegisterScene(SerializedObject so)
        {
            if (registered) return;
            registeredSerializedObject = so;
            sceneCallback = sv =>
            {
                try
                {
                    if (!viewAndEdit)
                        return;

                    // Defensive checks: if serialized object or properties are missing, unregister to avoid errors
                    if (registeredSerializedObject == null)
                    {
                        UnregisterScene();
                        return;
                    }

                    // If any of the cached properties are null (drawer lifecycle changed), unregister
                    if (p_distance == null || p_angle == null || p_transform == null)
                    {
                        UnregisterScene();
                        return;
                    }

                    // If the underlying target object(s) no longer exist, unregister
                    var targets = registeredSerializedObject.targetObjects;
                    if (targets == null || targets.Length == 0)
                    {
                        UnregisterScene();
                        return;
                    }

                    // Safe call to drawing routine
                    DrawEditableFlatCone();
                }
                catch (System.Exception ex)
                {
                    // Ensure we clean up on unexpected errors to prevent GUI state corruption
                    Debug.LogException(ex);
                    UnregisterScene();
                }
            };
            UnityEditor.SceneView.duringSceneGui += sceneCallback;
            registered = true;
        }

        private void UnregisterScene()
        {
            if (!registered) return;
            try
            {
                UnityEditor.SceneView.duringSceneGui -= sceneCallback;
            }
            catch
            {
                // ignore unsubscribe errors
            }
            registered = false;
            sceneCallback = null;
            registeredSerializedObject = null;
        }

        public void DrawEditableFlatCone()
        {
            // Guard: ensure serialized properties are still valid
            var so = p_distance?.serializedObject ?? p_angle?.serializedObject;
            if (so == null)
            {
                UnregisterScene();
                return;
            }

            // If the serialized object has been disposed or has no targets, stop drawing
            if (so.targetObjects == null || so.targetObjects.Length == 0)
            {
                UnregisterScene();
                return;
            }

            so.Update();

            // Read current values
            Transform transform = p_transform?.objectReferenceValue as Transform;
            if (transform == null)
            {
                // Nothing to draw; bail out quietly
                return;
            }

            float currentDistance = p_distance != null ? p_distance.floatValue : 0f;
            float currentHalfAngle = p_angle != null ? p_angle.floatValue : 0f;

            Color color = Color.green;
            try
            {
                if (p_viewColor != null)
                    color = p_viewColor.colorValue;
            }
            catch
            {
                // ignore if viewColor isn't serialized/present
            }

            // Prepare forward on horizontal plane
            Vector3 f = transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.0001f)
                f = transform.forward.normalized;
            f.Normalize();

            // Draw cone visuals (filled arc + wire + radius lines)
            float halfAngle = Mathf.Abs(currentHalfAngle);
            Vector3 startDir = Quaternion.AngleAxis(-halfAngle, transform.up) * f;

            Handles.color = color;
            Handles.DrawWireArc(transform.position, transform.up, startDir, halfAngle * 2f, currentDistance);
            Handles.DrawLine(transform.position, transform.position + (Quaternion.AngleAxis(-halfAngle, transform.up) * f) * currentDistance);
            Handles.DrawLine(transform.position, transform.position + (Quaternion.AngleAxis(halfAngle, transform.up) * f) * currentDistance);

            Handles.color = new Color(color.r, color.g, color.b, .12f);
            Handles.DrawSolidArc(transform.position, transform.up, startDir, halfAngle * 2f, currentDistance);

            // Interactive handles
            EditorGUI.BeginChangeCheck();

            // Distance slider handle
            Handles.color = color;
            Vector3 distHandlePos = transform.position + f * currentDistance;
            Vector3 newDistHandlePos = Handles.Slider(distHandlePos, f, HandleUtility.GetHandleSize(distHandlePos) * .15f, Handles.ConeHandleCap, 0f);
            float newDistance = currentDistance;
            {
                Vector3 delta = newDistHandlePos - transform.position;
                float projected = Vector3.Dot(delta, f);
                newDistance = Mathf.Max(0f, projected);
            }

            // Angle free-move handle
            float newHalfAngle = Mathf.Abs(currentHalfAngle);
            Vector3 rimDir = Quaternion.AngleAxis(halfAngle, transform.up) * f;
            Vector3 angleHandlePos = transform.position + rimDir * currentDistance;
            Handles.color = color;
            Vector3 newAngleHandlePos = Handles.FreeMoveHandle(angleHandlePos, HandleUtility.GetHandleSize(angleHandlePos) * 0.08f, Vector3.zero, Handles.SphereHandleCap);
            {
                Vector3 dir = newAngleHandlePos - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude >= 0.0001f)
                {
                    dir.Normalize();
                    float angleSigned = Vector3.SignedAngle(f, dir, transform.up);
                    newHalfAngle = Mathf.Abs(angleSigned);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (p_distance != null)
                    p_distance.floatValue = Mathf.Max(0f, newDistance);
                if (p_angle != null)
                    p_angle.floatValue = Mathf.Clamp(newHalfAngle, 0f, 180f);
                so.ApplyModifiedProperties();
                UnityEditor.SceneView.RepaintAll();
            }
        }

    }
#endif
}