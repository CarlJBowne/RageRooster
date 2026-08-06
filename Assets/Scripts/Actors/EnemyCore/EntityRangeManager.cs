using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static RageRooster.Services;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

public class EntityRangeManager : MonoBehaviour
{
    public List<Range> ranges = new();
    public float updateInterval = 1;
    public Transform targetOverride;
    public MessageSendType sendVisualEvents;
    public enum MessageSendType
    {
        SendNoEvents,
        SendFinalEvents,
        SendALLEvents
    }
    public UltEvents.UltEvent<string> ultEvents;
    public MessageSendType sendUltEvents;
    public string outerMostRangeName = "OutOfRange";

    [SerializeField] private bool showGizmos = false;

    public int CurrentRange { get; private set; }
    public float CurrentDistance { get; private set; }
    public float CurrentDOT { get; private set; }
    public Vector3 Target => targetOverride == null ? Player.Center : targetOverride.position;

    private float timer;

    private void Reset()
    {
        ranges.Add(new()
        {
            name = "Closest",
            outerThreshold = 10f,
            lineOfSightReq = 180f,
#if UNITY_EDITOR
            E_displayColor_EDITOR = Color.white
#endif
        });
    }

    private void OnEnable() => UpdateRange(true);
    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer >= updateInterval)
        {
            timer %= updateInterval;
            UpdateRange();
        }
    }

    public void UpdateRange(bool doEvenIfSame = false)
    {
        if (!Gameplay.Active) return;
        GetDistance();
        GetDOT();

        bool outCondition = OutwardCondition();
        bool inCondition = InwardCondition();

        if (!inCondition && !outCondition && !doEvenIfSame) return;

        int finalTarget = CurrentRange;

        while (outCondition)
        {
            CurrentRange++;
            outCondition = OutwardCondition();
            SendMessage(!outCondition);
        }
        while (inCondition)
        {
            CurrentRange--;
            inCondition = InwardCondition();
            SendMessage(!inCondition);
        }

    }

    public float GetDistance()
    {
        CurrentDistance = (Target - transform.position).magnitude;
        return CurrentDistance;
    }
    public float GetDOT()
    {
        CurrentDOT = (Vector3.Dot((Target - transform.position).normalized, transform.forward.normalized) - 1) * -90;
        return CurrentDOT;
    }
    public float GetRange(int i)
    {
        return i < 0 ? -1
            : i > ranges.Count ? float.PositiveInfinity
            : ranges[i];
    }

    public bool OutwardCondition() => CurrentRange != ranges.Count && CurrentDistance > ranges[CurrentRange];
    public bool InwardCondition() => CurrentRange != 0 && CurrentDistance < ranges[CurrentRange - 1] && CurrentDOT < ranges[CurrentRange - 1].lineOfSightReq;

    public void SendMessage(bool isFinal = false)
    {
        string result = CurrentRange == ranges.Count ? outerMostRangeName : ranges[CurrentRange];

        if (sendVisualEvents is MessageSendType.SendALLEvents || (sendVisualEvents is MessageSendType.SendFinalEvents && isFinal))
            RangeUpdateEvent.Trigger(result, gameObject);
        if (sendUltEvents is MessageSendType.SendALLEvents || (sendUltEvents is MessageSendType.SendFinalEvents && isFinal))
            ultEvents.Invoke(result);

    }

    [System.Serializable]
    public class Range
    {
        public string name;
        public float outerThreshold;
        [Range(1, 180)] public float lineOfSightReq = 180;

        public static implicit operator float(Range R) => R.outerThreshold;
        public static implicit operator string(Range R) => R.name;

#if UNITY_EDITOR
        public Color E_displayColor_EDITOR = new();
        public bool openInEditor_EDITOR = false;
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(EntityRangeManager))]
    private class Editor : UnityEditor.Editor
    {
        // PSEUDOCODE / PLAN (stored as comment inside file):
        // 1. Provide a serialized helper CreateNewRangeAt(int prevRange) that inserts a new serialized element
        //    after prevRange, initializes its fields and randomizes color. This centralizes array insertion logic.
        // 2. Re-order inspector so the ranges list is shown first, then the "Toggle Range Gizmos" button.
        // 3. The "Add Range" button will only be visible when there are zero ranges.
        // 4. Build per-element RangeDisplay UI:
        //      - Each element will be a horizontal row: [Foldout (flex-grow)] [Insert/Delete buttons]
        //      - Foldout contains the editable fields (Range, Name, LOS, optional Color)
        //      - Insert/Delete are outside the foldout on the right so they don't get clipped by foldout arrow
        //      - Foldout will have a left margin so its arrow doesn't draw outside the list box.
        // 5. For value changes (outerThreshold, LOS, Name, Color) update only the serialized property and SetValueWithoutNotify
        //    on the field to avoid rebuilding the whole list. Rebuild only on structural changes (insert/delete).
        // 6. Ensure Add button visibility and gizmo toggle placement updated in RebuildRangesUI.
        //
        // This implements the requested UI/behavior changes while keeping the SceneView gizmo logic intact.

        private EntityRangeManager TargetManager => (EntityRangeManager)target;
        private VisualElement root;
        private ScrollView rangesContainer;
        private UnityEditor.SerializedProperty rangesProp;
        private bool showGizmos = false;

        Foldout CentralConfig;
        private UnityEditor.SerializedProperty updateIntervalProp;
        private UnityEditor.SerializedProperty targetOverrideProp;
        private UnityEditor.SerializedProperty sendVisualEventsProp;
        private UnityEditor.SerializedProperty sendUltEventEnumProp;
        private UnityEditor.SerializedProperty ultEventsProp;
        private UnityEditor.SerializedProperty outmostRangeProp;
        private Label playInfoLabel;
        private System.Action rebuildAction;

        // Exposed UI button so we can toggle its visibility in RebuildRangesUI
        private Button addButton;
        private Button gizmoButton;

        private List<RangeDisplay> rangeDisplays = new();

        private void OnEnable()
        {
            rangesProp = serializedObject.FindProperty(nameof(ranges));
            updateIntervalProp = serializedObject.FindProperty(nameof(updateInterval));
            targetOverrideProp = serializedObject.FindProperty(nameof(targetOverride));
            sendVisualEventsProp = serializedObject.FindProperty(nameof(sendVisualEvents));
            sendUltEventEnumProp = serializedObject.FindProperty(nameof(sendUltEvents));
            ultEventsProp = serializedObject.FindProperty(nameof(ultEvents));
            outmostRangeProp = serializedObject.FindProperty(nameof(outerMostRangeName));


            rebuildAction = () =>
            {
                if (rangesContainer != null)
                {
                    RebuildRangesUI();
                }
            };

            UnityEditor.SceneView.duringSceneGui += SceneGUI;
            UnityEditor.EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            UnityEditor.SceneView.duringSceneGui -= SceneGUI;
            UnityEditor.EditorApplication.update -= OnEditorUpdate;
        }

        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();

            CentralConfig = new()
            {
                text = "Main Config"
            };
            root.Add(CentralConfig);

            CentralConfig.Add(new PropertyField(updateIntervalProp));
            CentralConfig.Add(new PropertyField(targetOverrideProp));
            CentralConfig.Add(new PropertyField(sendVisualEventsProp));

            var sendUltEventEnum = new PropertyField(sendUltEventEnumProp);
            var ultEvents = new PropertyField(ultEventsProp);

            CentralConfig.Add(sendUltEventEnum);
            CentralConfig.Add(ultEvents);

            UpdateUltEventsVisible();
            sendUltEventEnum.RegisterValueChangeCallback(UpdateUltEventsVisible);
            void UpdateUltEventsVisible(SerializedPropertyChangeEvent E = null) =>
                ultEvents.style.display = sendUltEventEnumProp.enumValueIndex != 0 ? DisplayStyle.Flex : DisplayStyle.None;

            var playBox = new Box();
            playBox.style.marginTop = 8;
            playBox.Add(new Label("Runtime Debug"));
            playInfoLabel = new Label();
            playBox.Add(playInfoLabel);
            root.Add(playBox);
            playBox.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;

            var rangeLabel = new Label("Ranges")
            {
                style =
                {
                    fontSize = 15,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            root.Add(rangeLabel);
            rangesContainer = new()
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible,
                style =
                {
                    maxHeight = 300,
                    minHeight = 30,
                }
            };

            addButton = new Button(() =>
            {
                // If no ranges, create as first element (prev = -1 so our helper will use fallback)
                CreateNewRangeAt(rangesProp.arraySize - 1);
            })
            {
                text = "Add Range",
                style =
                {
                    backgroundColor = Color.darkGreen
                }
            };

            root.Add(rangesContainer);

            root.Add(addButton);
            root.Add(new PropertyField(outmostRangeProp));

            gizmoButton = new Button(() =>
            {
                serializedObject.Update();
                showGizmos = !showGizmos;
                serializedObject.ApplyModifiedProperties();
                UnityEditor.SceneView.RepaintAll();
                RebuildRangesUI();
            })
            { text = "Toggle Range Gizmos" };
            root.Add(gizmoButton);


            RebuildRangesUI();
            return root;
        }

        // Centralized serialized insertion helper that mirrors EntityRangeManager.CreateNewRange but operates on serializedObject
        public void CreateNewRangeAt(int insert)
        {
            serializedObject.Update();
            int insertAt = insert + 1;
            if (insertAt < 0) insertAt = 0;
            rangesProp.InsertArrayElementAtIndex(insertAt);
            var newElem = rangesProp.GetArrayElementAtIndex(insertAt);
            var nameProp = newElem.FindPropertyRelative(nameof(Range.name));
            var outerProp = newElem.FindPropertyRelative(nameof(Range.outerThreshold));
            var losProp = newElem.FindPropertyRelative(nameof(Range.lineOfSightReq));
            var colorProp = newElem.FindPropertyRelative(nameof(Range.E_displayColor_EDITOR));
            var openProp = newElem.FindPropertyRelative(nameof(Range.openInEditor_EDITOR));

            nameProp.stringValue = "New Range";
            float prev = IDValid(insert) ? rangesProp.GetArrayElementAtIndex(insert).FindPropertyRelative(nameof(Range.outerThreshold)).floatValue : 0f;
            float next = IDValid(insert + 2) ? rangesProp.GetArrayElementAtIndex(insert + 2).FindPropertyRelative(nameof(Range.outerThreshold)).floatValue : prev + 20;

            bool IDValid(int i) => i >= 0 && i < rangesProp.arraySize;

            outerProp.floatValue = Mathf.Lerp(prev, next, .5f);
            losProp.floatValue = 180f;
            if (colorProp != null)
                colorProp.colorValue = UnityEngine.Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f);

            if (openProp != null)
                openProp.boolValue = insertAt == 0 ||
                    rangesProp.GetArrayElementAtIndex(insertAt).FindPropertyRelative(nameof(Range.openInEditor_EDITOR)).boolValue;

            serializedObject.ApplyModifiedProperties();
            RebuildRangesUI();
        }

        private void OnEditorUpdate()
        {
            if (playInfoLabel != null)
            {
                if (UnityEditor.EditorApplication.isPlaying)
                {
                    playInfoLabel.text = $"CurrentRange: {TargetManager.CurrentRange}\nCurrentDistance: {TargetManager.CurrentDistance:F2}\nCurrentDOT: {TargetManager.CurrentDOT:F2}";
                }
                else
                {
                    playInfoLabel.text = "Enter Play Mode to view runtime values.";
                }
            }
        }

        private void RebuildRangesUI()
        {
            rangesContainer.Clear();
            rangeDisplays.Clear();

            serializedObject.Update();

            for (int i = 0; i < rangesProp.arraySize; i++)
            {
                var elem = rangesProp.GetArrayElementAtIndex(i);
                var display = new RangeDisplay(this, elem, i);
                rangeDisplays.Add(display);
                rangesContainer.Add(display);
            }

            // Add button visibility: only visible when there are zero ranges
            if (addButton != null)
            {
                addButton.style.display = rangesProp.arraySize > 0 ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // Update gizmo button visual state if needed (no-op beyond ensuring it's present)
            if (gizmoButton != null)
            {
                // keep gizmoButton available; no further action necessary here
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SceneGUI(UnityEditor.SceneView sceneView)
        {
            var manager = TargetManager;
            if (manager == null) return;

            // Use the serialized property for showGizmos to ensure toggle in inspector works
            serializedObject.Update();
            bool show = showGizmos;
            serializedObject.ApplyModifiedProperties();

            if (!show) return;

            Vector3 center = manager.transform.position;

            float maxAngle = 180;

            string outestName = outmostRangeProp.stringValue;

            // Draw a wire disc for each range and LOS direction lines when LOS < 180
            for (int i = 0; i < rangesProp.arraySize; i++)
            {
                SerializedProperty elem = rangesProp.GetArrayElementAtIndex(i);
                SerializedProperty outerProp = elem.FindPropertyRelative(nameof(Range.outerThreshold));
                SerializedProperty losProp = elem.FindPropertyRelative(nameof(Range.lineOfSightReq));
                SerializedProperty nameProp = elem.FindPropertyRelative(nameof(Range.name));
                SerializedProperty colorProp = elem.FindPropertyRelative(nameof(Range.E_displayColor_EDITOR));
                Color col = colorProp != null ? colorProp.colorValue : Color.white;
                float radius = outerProp.floatValue;
                float los = Mathf.Min(losProp.floatValue, maxAngle);

                // Draw full wire disc at radius
                UnityEditor.Handles.color = col;
                UnityEditor.Handles.DrawWireDisc(center, Vector3.up, radius, 5);

                // If LOS less than 180, draw two white lines at +/- half the LOS angle
                if (los < 180f - 0.0001f)
                {
                    float halfAngle = los;
                    Vector3 forward = manager.transform.forward;

                    Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.up);
                    Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.up);

                    Vector3 leftDir = leftRot * forward;
                    Vector3 rightDir = rightRot * forward;

                    Color prevColor = UnityEditor.Handles.color;
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.DrawLine(center, center + leftDir.normalized * radius);
                    UnityEditor.Handles.DrawLine(center, center + rightDir.normalized * radius);
                    UnityEditor.Handles.color = prevColor;
                }

                // Draw handle at center + forward * radius using a small rectangle handle
                Vector3 handleDir = Quaternion.AngleAxis(0f, Vector3.up) * manager.transform.forward;
                Vector3 handlePos = center + handleDir.normalized * radius;

                EditorGUI.BeginChangeCheck();
                Vector3 newHandlePos = UnityEditor.Handles.Slider(handlePos, handleDir.normalized, HandleUtility.GetHandleSize(handlePos) * 0.22f, UnityEditor.Handles.CubeHandleCap, 0f);

                GUIStyle style = new()
                {
                    fontSize = 16,
                    normal = { textColor = colorProp.colorValue }
                };

                UnityEditor.Handles.Label(handlePos - (manager.transform.forward * 1.5f), nameProp.stringValue, style);

                if (i == rangesProp.arraySize - 1)
                {
                    GUIStyle lastStyle = new()
                    {
                        fontSize = 16,
                        normal = { textColor = Color.white }
                    };
                    UnityEditor.Handles.Label(handlePos + (manager.transform.forward * 3), outestName, lastStyle);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    float newRadius = (newHandlePos - center).magnitude;

                    // Determine clamping bounds based on neighbors
                    float minAllowed = (i - 1) >= 0 ? rangesProp.GetArrayElementAtIndex(i - 1).FindPropertyRelative("outerThreshold").floatValue + 0.1f : 0.1f;
                    float maxAllowed = (i + 1) < rangesProp.arraySize ? rangesProp.GetArrayElementAtIndex(i + 1).FindPropertyRelative("outerThreshold").floatValue - 0.1f : float.PositiveInfinity;
                    newRadius = Mathf.Max(newRadius, minAllowed);
                    if (!float.IsPositiveInfinity(maxAllowed)) newRadius = Mathf.Min(newRadius, maxAllowed);

                    // Apply to serialized property
                    serializedObject.Update();
                    outerProp.floatValue = newRadius;
                    serializedObject.ApplyModifiedProperties();

                    // Mark dirty and repaint
                    UnityEditor.EditorUtility.SetDirty(manager);
                    UnityEditor.SceneView.RepaintAll();

                    // Ensure the inspector's FloatField updates immediately for this range if we have a matching display
                    if (i >= 0 && i < rangeDisplays.Count)
                    {
                        rangeDisplays[i].RefreshFromSerialized();
                    }
                }
            }


        }

        private class RangeDisplay : VisualElement
        {
            private Foldout primaryFoldout;
            private FloatField rangeField;
            private TextField nameField;
            private FloatField losField;
            private ColorField colorField;
            private VisualElement btnRow;
            private Button insertButton;
            private Button removeButton;

            private SerializedProperty elementProp;
            private SerializedProperty outerProp;
            private SerializedProperty nameProp;
            private SerializedProperty losProp;
            private SerializedProperty colorProp;
            private SerializedProperty openProp;
            private Editor parentEditor;
            private int indexInArray;

            public RangeDisplay(Editor parent, SerializedProperty element, int index)
            {
                parentEditor = parent;
                elementProp = element;
                indexInArray = index;

                // Cache sub-properties
                outerProp = element.FindPropertyRelative(nameof(Range.outerThreshold));
                nameProp = element.FindPropertyRelative(nameof(Range.name));
                losProp = element.FindPropertyRelative(nameof(Range.lineOfSightReq));
                colorProp = element.FindPropertyRelative(nameof(Range.E_displayColor_EDITOR));
                openProp = element.FindPropertyRelative(nameof(Range.openInEditor_EDITOR));

                BuildUI();
                SyncFromSerialized();
            }

            private void BuildUI()
            {
                string title = $"#{indexInArray}: {nameProp.stringValue}";
                primaryFoldout = new Foldout { text = title, value = openProp != null ? openProp.boolValue : false };
                primaryFoldout.style.marginBottom = 4;
                // shift foldout content slightly to the right so the foldout's arrow doesn't render outside the list box
                primaryFoldout.style.marginLeft = 8;
                primaryFoldout.style.flexGrow = 1;
                primaryFoldout.style.minWidth = 0; // allow shrink

                // Range field
                rangeField = new FloatField("Range");
                rangeField.RegisterValueChangedCallback(evt =>
                {
                    HandleRangeChanged(evt.newValue);
                });
                primaryFoldout.Add(rangeField);

                // Name field
                nameField = new TextField("Name");
                nameField.RegisterValueChangedCallback(evt =>
                {
                    parentEditor.serializedObject.Update();
                    nameProp.stringValue = evt.newValue;
                    parentEditor.serializedObject.ApplyModifiedProperties();
                    primaryFoldout.text = $"#{indexInArray}: {nameProp.stringValue}";
                });
                primaryFoldout.Add(nameField);

                // LOS field
                losField = new FloatField("Line Of Sight");
                losField.RegisterValueChangedCallback(evt =>
                {
                    parentEditor.serializedObject.Update();
                    float v = Mathf.Clamp(evt.newValue, 0f, 180f);
                    losProp.floatValue = v;
                    parentEditor.serializedObject.ApplyModifiedProperties();
                    losField.SetValueWithoutNotify(v);
                    UnityEditor.SceneView.RepaintAll();
                });
                primaryFoldout.Add(losField);

                // Color field only shown when gizmos toggled
                colorField = new ColorField("Gizmo Color");
                colorField.RegisterValueChangedCallback(evt =>
                {
                    parentEditor.serializedObject.Update();
                    if (colorProp != null) colorProp.colorValue = evt.newValue;
                    parentEditor.serializedObject.ApplyModifiedProperties();
                    UnityEditor.SceneView.RepaintAll();
                });

                // Buttons
                btnRow = new VisualElement()
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = Justify.FlexEnd,
                        position = Position.Absolute,
                        top = 0,
                        right = 0
                    }
                };

                insertButton = new Button(() => { parentEditor.CreateNewRangeAt(indexInArray); })
                {
                    text = "+",
                    style =
                    {
                        backgroundColor = Color.darkGreen,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        flexGrow = .05f,
                        maxHeight = 15,
                        borderRightWidth = 0,
                        marginRight = 0,
                        borderBottomRightRadius = 0,
                        borderTopRightRadius = 0,
                    }
                };
                btnRow.Add(insertButton);

                removeButton = new Button(() =>
                {
                    parentEditor.serializedObject.Update();
                    if (parentEditor.rangesProp.arraySize > 0)
                    {
                        parentEditor.rangesProp.DeleteArrayElementAtIndex(indexInArray);
                        parentEditor.serializedObject.ApplyModifiedProperties();
                        parentEditor.RebuildRangesUI();
                    }
                })
                {
                    text = "-",
                    style =
                    {
                        backgroundColor = Color.darkRed,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        flexGrow = .05f,
                        maxHeight = 15,
                        borderLeftWidth = 0,
                        marginLeft = 0,
                        borderBottomLeftRadius = 0,
                        borderTopLeftRadius = 0,
                    }
                };
                btnRow.Add(removeButton);

                // Add foldout and buttons to row
                this.Add(primaryFoldout);
                this.Add(btnRow);

                // Foldout open state persistence
                primaryFoldout.RegisterValueChangedCallback(evt =>
                {
#if UNITY_EDITOR
                    parentEditor.serializedObject.Update();
                    if (openProp != null) openProp.boolValue = evt.newValue;
                    parentEditor.serializedObject.ApplyModifiedProperties();
#endif
                });
            }

            private void SyncFromSerialized()
            {
                parentEditor.serializedObject.Update();
                // indexInArray may be out of date if rebuild happens; title updated by parent rebuild anyway.
                rangeField.SetValueWithoutNotify(outerProp.floatValue);
                nameField.SetValueWithoutNotify(nameProp.stringValue);
                losField.SetValueWithoutNotify(losProp.floatValue);
                if (parentEditor.showGizmos)
                {
                    if (!primaryFoldout.Contains(colorField))
                        primaryFoldout.Add(colorField);
                    if (colorProp != null) colorField.SetValueWithoutNotify(colorProp.colorValue);
                }
                else
                {
                    if (primaryFoldout.Contains(colorField))
                        primaryFoldout.Remove(colorField);
                }
                if (openProp != null)
                    primaryFoldout.SetValueWithoutNotify(openProp.boolValue);
                parentEditor.serializedObject.ApplyModifiedProperties();
            }

            // Public refresh wrapper so Scene GUI can cause the displayed FloatField to update immediately.
            public void RefreshFromSerialized()
            {
                SyncFromSerialized();
            }

            private void HandleRangeChanged(float newValue)
            {
                // Clamp against neighbors without forcing UI rebuilds.
                parentEditor.serializedObject.Update();

                // Re-compute index in case structural changes occurred. Find this element's index.
                int currentIndex = -1;
                for (int i = 0; i < parentEditor.rangesProp.arraySize; i++)
                {
                    if (parentEditor.rangesProp.GetArrayElementAtIndex(i).propertyPath == elementProp.propertyPath)
                    {
                        currentIndex = i;
                        break;
                    }
                }
                if (currentIndex == -1) currentIndex = indexInArray; // fallback

                float minAllowed = (currentIndex - 1) >= 0 ? parentEditor.rangesProp.GetArrayElementAtIndex(currentIndex - 1).FindPropertyRelative("outerThreshold").floatValue + 0.1f : 0.1f;
                float maxAllowed = (currentIndex + 1) < parentEditor.rangesProp.arraySize ? parentEditor.rangesProp.GetArrayElementAtIndex(currentIndex + 1).FindPropertyRelative("outerThreshold").floatValue - 0.1f : float.PositiveInfinity;

                float v = Mathf.Max(newValue, minAllowed);
                if (!float.IsPositiveInfinity(maxAllowed)) v = Mathf.Min(v, maxAllowed);

                outerProp.floatValue = v;
                parentEditor.serializedObject.ApplyModifiedProperties();

                // Update the displayed value without triggering another change event.
                rangeField.SetValueWithoutNotify(v);

                // Ensure SceneView/gizmos updated
                UnityEditor.SceneView.RepaintAll();
                UnityEditor.EditorUtility.SetDirty(parentEditor.TargetManager);
            }
        }
    }
#endif
}

[UnitTitle("Range Update Event"), UnitCategory("Events/Entity")]
public class RangeUpdateEvent : EventUnit<string>
{
    public const string EVENT_NAME = "RangeUpdateEvent";

    [DoNotSerialize]
    public ValueInput NameInput;
    [DoNotSerialize]
    public ValueOutput NameOutput;
    protected override bool register => true;

    public static void Trigger(string name, GameObject Object) => EventBus.Trigger(new(EVENT_NAME, Object), name);

    protected override void Definition()
    {
        base.Definition();
        NameInput = ValueInput("Name", string.Empty);
        NameOutput = ValueOutput<string>("RangeName");
    }
    public override EventHook GetHook(GraphReference reference) => new(EVENT_NAME, reference.gameObject);

    protected override void AssignArguments(Flow flow, string arg) => flow.SetValue(NameOutput, flow.GetValue<string>(NameInput));

    protected override bool ShouldTrigger(Flow flow, string arg)
    {
        string expected = flow.GetValue<string>(NameInput);

        return string.IsNullOrEmpty(expected) || string.Equals(expected, arg);
    }
}