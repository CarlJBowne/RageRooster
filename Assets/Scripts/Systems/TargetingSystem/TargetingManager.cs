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
    public static TargetingManager Instance { get; private set; }
    private static TargetingChannel<InteractionTarget> InteractionChannel;
    private static TargetingChannel<MeleeTarget> MeleeChannel;
    private static TargetingChannel<RangedTarget> RangedChannel;

    public static Transform InteractionPopup {  get; private set; }

    #region Instance Fields

    [SerializeField] private TargetingRange rangedHipFireRange = new();
    [SerializeField] private TargetingRange rangedAimingRange = new(); 
    [SerializeField] private TargetingRange grabbingRange = new(); 
    [SerializeField] private TargetingRange interactionRange = new();
    [SerializeField] private Transform interactionPopup;

    #endregion

    // Public API: manage active targets
    public static void AddActiveTarget(Target target)
    {
        if (target is MeleeTarget meleeTarget)
        {
            if (!MeleeChannel.ALLTARGETS.Contains(meleeTarget)) 
                MeleeChannel.ALLTARGETS.Add(meleeTarget);
        }
        else if (target is RangedTarget rangedTarget)
        {
            if (!RangedChannel.ALLTARGETS.Contains(rangedTarget))
                RangedChannel.ALLTARGETS.Add(rangedTarget);
        }
        else if (target is InteractionTarget interactionTarget)
        {
            if (!InteractionChannel.ALLTARGETS.Contains(interactionTarget))
                InteractionChannel.ALLTARGETS.Add(interactionTarget);
        }
    }

    public static void RemoveActiveTarget(Target target)
    {
        if (target is MeleeTarget meleeTarget)
        {
            if (MeleeChannel.ALLTARGETS.Contains(meleeTarget))
                MeleeChannel.ALLTARGETS.Remove(meleeTarget);
        }
        else if (target is RangedTarget rangedTarget)
        {
            if (RangedChannel.ALLTARGETS.Contains(rangedTarget))
                RangedChannel.ALLTARGETS.Remove(rangedTarget);
        }
        else if (target is InteractionTarget interactionTarget)
        {
            if (InteractionChannel.ALLTARGETS.Contains(interactionTarget))
                InteractionChannel.ALLTARGETS.Remove(interactionTarget);
        }
    }

    public static void ToggleAimingDownSights(bool value) => RangedChannel.ChangeRange(value ? Instance.rangedAimingRange : Instance.rangedHipFireRange);



    // Unity lifecycle
    private void Awake()
    {
        Instance = this;

        InteractionChannel = new(interactionRange);
        MeleeChannel = new(grabbingRange);
        RangedChannel = new(rangedHipFireRange);

        InteractionPopup = interactionPopup;
    }


    private void FixedUpdate()
    {
        InteractionChannel.CalculateTargets();
        MeleeChannel.CalculateTargets();
        RangedChannel.CalculateTargets();
    }

    public class TargetingChannel<T> where T : Target
    {
        public List<T> ALLTARGETS { get; private set; }
        public T CurrentTarget { get; private set; }
        public TargetingRange Range { get; private set; }

        public TargetingChannel(TargetingRange range)
        {
            ALLTARGETS = new();
            CurrentTarget = null;
            Range = range;
        }

        public void CalculateTargets()
        {
            int chosenIndex = -1;
            float closestScore = 5f; //Max possible score is 2 (1 distance + 1 angle)

            for (int i = 0; i < ALLTARGETS.Count; i++)
            {
                if (ALLTARGETS[i] == null || ALLTARGETS[i].enabled == false) continue;

                float distance = ALLTARGETS[i].GetDistance(Range);
                float angle = ALLTARGETS[i].GetAngle(Range);

                if (distance > Range.maxDistance || angle > Range.maxAngle)
                {
                    ALLTARGETS[i].TargetState = Target.TargetStates.OutOfRange;
                    continue;
                }
                ALLTARGETS[i].TargetState = Target.TargetStates.WithinRange;

                float distanceScore = distance / Range.maxDistance;
                float angleScore = angle / Range.maxAngle;
                float finalScore = (distanceScore * Range.distanceAngleWeighting) + (angleScore * (1f - Range.distanceAngleWeighting));

                if (finalScore < closestScore)
                {
                    closestScore = finalScore;
                    chosenIndex = i;
                }
            }
            var ChosenTarget = chosenIndex != -1 ? ALLTARGETS[chosenIndex] : null;

            if(ChosenTarget != CurrentTarget)
            {
                if(CurrentTarget != null) CurrentTarget.TargetState = Target.TargetStates.WithinRange;
                CurrentTarget = ChosenTarget;
                if(CurrentTarget != null) CurrentTarget.TargetState = Target.TargetStates.Targeted;
            }

        }

        public void ChangeRange(TargetingRange newRange) => Range = newRange;
    }

    // Main selection logic


    public void AttemptInteract()
    {
        InteractionTarget target = InteractionChannel.CurrentTarget;
        if (target != null) target.Interact();
    }

}

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
    [UnityEditor.CustomPropertyDrawer(typeof(TargetingRange))]
    public class Editor : PropertyDrawer
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
            p_transform = property.FindPropertyRelative(nameof(front));
            p_distance = property.FindPropertyRelative(nameof(maxDistance));
            p_angle = property.FindPropertyRelative(nameof(maxAngle));
            p_distanceAngleWeighting = property.FindPropertyRelative(nameof(distanceAngleWeighting));
            p_viewColor = property.FindPropertyRelative("viewColor");

            var root = new VisualElement();

            // Row container: left = foldout (expands to show fields), right = column (button + color box)
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 4;
            row.style.marginTop = 2;

            // Foldout acts like the dropdown header + content container
            var foldout = new Foldout
            {
                text = property.displayName,
                value = false // collapsed by default
            };
            // Make foldout grow to take available width
            foldout.style.flexGrow = 1;
            foldout.style.minWidth = 0; // allow shrinking in narrow inspectors

            // Content inside foldout: the actual property fields
            var content = new VisualElement();
            content.style.flexDirection = FlexDirection.Column;
            content.style.paddingLeft = 12;
            content.style.paddingTop = 2;
            content.style.paddingBottom = 2;

            if (p_transform != null)
            {
                var transformField = new UnityEditor.UIElements.PropertyField(p_transform);
                transformField.SetEnabled(true);
                content.Add(transformField);
            }

            if (p_distance != null)
            {
                var distanceField = new UnityEditor.UIElements.PropertyField(p_distance);
                distanceField.SetEnabled(true);
                content.Add(distanceField);
            }

            if (p_angle != null)
            {
                var angleField = new UnityEditor.UIElements.PropertyField(p_angle);
                angleField.SetEnabled(true);
                content.Add(angleField);
            }

            if (p_distanceAngleWeighting != null)
            {
                var weightingField = new UnityEditor.UIElements.PropertyField(p_distanceAngleWeighting);
                weightingField.SetEnabled(true);
                content.Add(weightingField);
            }

            // NOTE: Do NOT add the viewColor to `content`. We'll show a compact color box below the View/Edit button instead.

            foldout.Add(content);

            // Right side: vertical column for the button and small color box
            var rightColumn = new VisualElement();
            rightColumn.style.flexDirection = FlexDirection.Column;
            rightColumn.style.alignItems = Align.FlexEnd;
            rightColumn.style.justifyContent = Justify.FlexStart;

            // View/Edit button to the right of the foldout header
            Button viewEditButton = null;
            viewEditButton = new Button(() => ToggleViewEdit(viewEditButton))
            {text = "View/Edit"};
            viewEditButton.style.marginLeft = 6;
            viewEditButton.style.flexShrink = 0;
            viewEditButton.style.alignSelf = Align.FlexStart;

            rightColumn.Add(viewEditButton);

            // Small color box (no label) under the button
            // Show this box only when the foldout is expanded. Make it a wider square (approx 22x22).
            if (p_viewColor != null)
            {
                // Use UIElements ColorField bound to the serialized property path so root.Bind will work.
                var smallColor = new ColorField();
                smallColor.bindingPath = p_viewColor.propertyPath;
                smallColor.SetEnabled(true);
                // Hide label by setting it to empty and adjust size
                smallColor.label = string.Empty;
                smallColor.style.width = 22;
                smallColor.style.height = 22;
                smallColor.style.marginTop = 6;
                smallColor.style.marginRight = 2;
                smallColor.style.flexShrink = 0;
                // Initially hidden when foldout is collapsed
                smallColor.style.display = foldout.value ? DisplayStyle.Flex : DisplayStyle.None;
                rightColumn.Add(smallColor);

                // Update visibility when foldout toggles
                foldout.RegisterValueChangedCallback(evt =>
                {
                    smallColor.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });
            }
            else
            {
                // Fallback: small neutral box if property isn't present
                var fallbackBox = new VisualElement();
                fallbackBox.style.width = 22;
                fallbackBox.style.height = 22;
                fallbackBox.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
                fallbackBox.style.marginTop = 6;
                fallbackBox.style.marginRight = 2;
                fallbackBox.style.flexShrink = 0;
                fallbackBox.style.display = foldout.value ? DisplayStyle.Flex : DisplayStyle.None;
                rightColumn.Add(fallbackBox);

                foldout.RegisterValueChangedCallback(evt =>
                {
                    fallbackBox.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });
            }

            row.Add(foldout);
            row.Add(rightColumn);

            root.Add(row);

            // Ensure binding so property fields are editable with Undo support
            var so = property.serializedObject;
            if (so != null)
            {
                root.Bind(so);
            }

            // Unregister scene callback when inspector element is detached/destroyed
            root.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                // Ensure we remove any registered scene callback to avoid leaks
                UnregisterScene();
            });

            return root;

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
        }

        private void RegisterScene()
        {
            if (registered) return;
            sceneCallback = sv =>
            {
                // Only draw when viewAndEdit is true (defensive)
                if (viewAndEdit)
                {
                    DrawEditableFlatCone();
                }
            };
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

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Default IMGUI fallback — leave empty to use UIElements inspector created in CreatePropertyGUI.
        }


        public void DrawEditableFlatCone()
        {
            var so = p_distance?.serializedObject ?? p_angle?.serializedObject;
            if (so == null) return;
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