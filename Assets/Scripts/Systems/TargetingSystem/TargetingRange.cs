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
                    RegisterScene();
                }
                else
                {
                    b.style.backgroundColor = new StyleColor(new Color(0.32f, 0.32f, 0.32f));
                    b.style.color = new StyleColor(Color.white);
                    UnregisterScene();
                }

                UnityEditor.SceneView.RepaintAll();
            }


            foldout.Bind(property.serializedObject);
            return foldout;
        }

        private void RegisterScene()
        {
            if (registered) return;
            sceneCallback = sv =>
            { if (viewAndEdit) DrawEditableFlatCone(); };
            UnityEditor.SceneView.duringSceneGui += sceneCallback;
            registered = true;
        }

        private void UnregisterScene()
        {
            if (!registered) return;
            UnityEditor.SceneView.duringSceneGui -= sceneCallback;
            registered = false;
            sceneCallback = null;
        }

        public void DrawEditableFlatCone()
        {
            var so = p_distance?.serializedObject ?? p_angle?.serializedObject;
            if (so == null)
            {
                UnregisterScene();
                return;
            }
            so.Update();

            // Read current values
            Transform transform = p_transform?.objectReferenceValue as Transform;
            if (transform == null) return;

            float currentDistance = p_distance.floatValue;
            float currentHalfAngle = p_angle.floatValue;

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
                p_distance.floatValue = Mathf.Max(0f, newDistance);
                p_angle.floatValue = Mathf.Clamp(newHalfAngle, 0f, 180f);
                so.ApplyModifiedProperties();
                UnityEditor.SceneView.RepaintAll();
            }
        }

    }
#endif
}