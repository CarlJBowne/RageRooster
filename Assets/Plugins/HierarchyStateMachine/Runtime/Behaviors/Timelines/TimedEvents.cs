using SLS.StateMachineH.SerializedDictionary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
#if ULT_EVENTS
using UltEvents;
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

namespace SLS.StateMachineH.Timelines
{
    public class TimedEvents : StateTimeline
    {
        [System.Serializable]
        public struct TimedEvent
        {
            public float time;
            public EVENT output;
        }
        public List<TimedEvent> events = new();
        public bool loopAfterLastEvent;

        int nextEventID = 0;

        protected override void OnSetup()
        {
            base.OnSetup();
#if UNITY_EDITOR
            if (events.Count == 0)
                Debug.LogWarning($"TimedEvents on State '{State.name}' in StateMachine '{Machine.name}' has no events configured and will not function.");
#endif
        }

        protected override void OnBegin()
        {
            elapsedTime = 0f;
            nextEventID = 0;
            if (events.Count > 0 && events[0].time == 0f)
            {
                events[0].output?.Invoke();
                nextEventID++;
            }
        }

        protected override void OnTick(float delta)
        {
            if (events.Count == 0) return;

            if (nextEventID < events.Count && WasPointPassed(events[nextEventID].time))
            {
                events[nextEventID].output?.Invoke();
                nextEventID++;
                if (nextEventID >= events.Count && loopAfterLastEvent)
                {
                    elapsedTime %= events[^1].time;
                    nextEventID = 0;
                }
            }

        }

#if UNITY_EDITOR
        [CustomEditor(typeof(TimedEvents))]
        public class Editor : UnityEditor.Editor
        {
            SerializedProperty eventsListProperty;
            Label emptyLabel;
            VisualElement rowsContainer;
            VisualElement root;

            private string noEventsHelpBoxText = "No timed events have been added. This system will not work without at least one. Click the + button to add one.";

            public override VisualElement CreateInspectorGUI()
            {
                serializedObject.Update();
                eventsListProperty = serializedObject.FindProperty(nameof(TimedEvents.events));

                root = new();
                // Top toolbar with add button and loop toggle

                rowsContainer = new();
                root.Add(rowsContainer);

                VisualElement bottomToolbar = new()
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                    }
                };


                SerializedProperty currentLoopValue = serializedObject.FindProperty(nameof(TimedEvents.loopAfterLastEvent));
                Button loopButton = null;
                loopButton = new(UpdateLoop)
                {
                    text = "Loop",
                    style =
                    {
                        backgroundColor = currentLoopValue.boolValue ? Color.gray5 : Color.gray2,
                        color = currentLoopValue.boolValue ? Color.white : Color.gray4,
                        width = 80,
                        height = 18,
                    }
                };
                void UpdateLoop()
                {
                    currentLoopValue.boolValue = !currentLoopValue.boolValue;
                    loopButton.style.backgroundColor = currentLoopValue.boolValue ? Color.gray5 : Color.gray2;
                    loopButton.style.color = currentLoopValue.boolValue ? Color.white : Color.gray4;
                    serializedObject.ApplyModifiedProperties();
                }
                bottomToolbar.Add(loopButton);

                Button addButton = new(AddElement)
                {
                    text = "+",
                    style =
                    {
                        backgroundColor = new StyleColor(new Color(0.2f, 0.6f, 0.2f)),
                        color = new StyleColor(Color.white),
                        width = 24,
                        height = 18,
                        position = Position.Absolute,
                        right = -2,
                        borderBottomRightRadius = 10,
                        borderBottomLeftRadius = 10,
                    }
                };
                MakeHighlightable(addButton);
                bottomToolbar.Add(addButton);

                root.Add(bottomToolbar);

                /*Toggle loopToggle = new("Loop?")
                {
                    bindingPath = nameof(TimedEvents.loopAfterLastEvent),
                    style =
                    {
                        marginLeft = 6
                    }
                };
                root.Add(addButton);
                root.Add(loopToggle);*/

                emptyLabel = new Label(noEventsHelpBoxText)
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        paddingLeft = 2,
                        paddingTop = 2,
                        paddingBottom = 2,
                    }
                };

                RefreshList();

                serializedObject.ApplyModifiedProperties();
                return root;
            }

            class TimedEventRow : VisualElement
            {
                FloatField timeField;
                PropertyField outputField;
                Button deleteBtn;
                int ID = -1;
                SerializedProperty timeProp;
                SerializedProperty outputProp;

                public TimedEventRow(int id, SerializedProperty prop, System.Action<int> onTimeChanged, System.Action<int> onDelete)
                {
                    ID = id;
                    timeProp = prop.FindPropertyRelative(nameof(TimedEvent.time));
                    outputProp = prop.FindPropertyRelative(nameof(TimedEvent.output));

                    style.flexDirection = FlexDirection.Row;
                    style.alignItems = Align.Center;
                    style.marginBottom = 2;

                    timeField = new FloatField()
                    {
                        label = " ",
                        pickingMode = PickingMode.Position,
                        style =
                        {
                            flexBasis = new StyleLength(new Length(20, LengthUnit.Percent)),
                            minHeight = 20,
                            left = -16
                        },
                        isDelayed = true,
                    };
                    timeField.labelElement.style.maxWidth = 8;
                    timeField.labelElement.style.minWidth = 8;
                    // initialize displayed value without triggering change events
                    timeField.SetValueWithoutNotify(timeProp.floatValue);
                    timeField.RegisterValueChangedCallback(evt =>
                    {
                        timeProp.floatValue = evt.newValue >= 0 ? evt.newValue : 0;
                        if (evt.newValue < 0) timeField.SetValueWithoutNotify(0);
                        timeProp.serializedObject.ApplyModifiedProperties();
                        onTimeChanged?.Invoke(ID);
                    });

                    outputField = new PropertyField(outputProp, string.Empty)
                    {
                        style =
                        {
                            flexGrow = 1,
                            flexBasis = new StyleLength(new Length(0, LengthUnit.Percent)),
                            marginLeft = -20
                        }
                    };
                    deleteBtn = new(() => onDelete?.Invoke(ID))
                    {
                        style =
                        {
                            paddingLeft = 0,
                            paddingRight = 0,
                            minWidth = 10,
                            minHeight = 21,
                            height = 18,
                            borderBottomLeftRadius = 0,
                            marginLeft = 0,
                            marginRight = 0,
                            flexShrink = 0,
                            backgroundColor = new UnityEngine.Color(0.85f, 0.2f, 0.2f),
                        }
                    };
                    MakeHighlightable(deleteBtn);

                    Add(timeField);
                    Add(outputField);
                    Add(deleteBtn);
                }
            }


            void RefreshList()
            {
                if (rowsContainer == null) return;
                rowsContainer.Clear();
                serializedObject.Update();

                eventsListProperty ??= serializedObject.FindProperty(nameof(TimedEvents.events));

                if (eventsListProperty == null || eventsListProperty.arraySize == 0)
                {
                    rowsContainer.Add(emptyLabel);
                }
                else
                {
                    for (int i = 0; i < eventsListProperty.arraySize; i++)
                        rowsContainer.Add(new TimedEventRow(i, eventsListProperty.GetArrayElementAtIndex(i), ReorderElements, RemoveElement));
                }
                root.Bind(serializedObject);
            }

            void AddElement()
            {
                serializedObject.Update();
                eventsListProperty ??= serializedObject.FindProperty(nameof(TimedEvents.events));
                if (eventsListProperty == null) return;

                int oldSize = eventsListProperty.arraySize;
                eventsListProperty.arraySize++;

                if (oldSize > 0)
                {
                    var prevTime = TimeFromIndex(oldSize - 1).floatValue;
                    TimeFromIndex(eventsListProperty.arraySize - 1).floatValue = prevTime + 0.0005f;
                }
                else
                {
                    TimeFromIndex(eventsListProperty.arraySize - 1).floatValue = 0f;
                }

                serializedObject.ApplyModifiedProperties();
                RefreshList();
            }

            void RemoveElement(int index)
            {
                if (index < 0) return;
                serializedObject.Update();
                eventsListProperty ??= serializedObject.FindProperty(nameof(TimedEvents.events));
                if (eventsListProperty == null || index >= eventsListProperty.arraySize) return;

                eventsListProperty.DeleteArrayElementAtIndex(index);
                // If it was a managed reference, DeleteArrayElementAtIndex will set null first; call again to remove
                if (index < eventsListProperty.arraySize && eventsListProperty.GetArrayElementAtIndex(index).propertyType == SerializedPropertyType.ObjectReference)
                {
                    // noop - keep simple; the typical UnityEvent array element deletion above is enough
                }
                serializedObject.ApplyModifiedProperties();
                RefreshList();
            }

            void ReorderElements(int index)
            {
                serializedObject.Update();
                eventsListProperty ??= serializedObject.FindProperty(nameof(TimedEvents.events));
                if (eventsListProperty == null || index < 0 || index >= eventsListProperty.arraySize) return;

                float thisTime = TimeFromIndex(index).floatValue;

                float prevTime = index > 0 ? TimeFromIndex(index - 1).floatValue : float.NegativeInfinity;
                float nextTime = index < eventsListProperty.arraySize - 1 ? TimeFromIndex(index + 1).floatValue : float.PositiveInfinity;

                if (thisTime > prevTime && thisTime < nextTime) return; // already in correct position

                int i = 0;
                for (; i < eventsListProperty.arraySize; i++)
                {
                    if (thisTime < TimeFromIndex(i).floatValue)
                        break;
                }

                int dest = Mathf.Clamp(i, 0, eventsListProperty.arraySize - 1);
                if (dest != index)
                {
                    eventsListProperty.MoveArrayElement(index, dest);
                    serializedObject.ApplyModifiedProperties();
                }

                RefreshList();
            }

            SerializedProperty TimeFromIndex(int i) => eventsListProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(TimedEvent.time));
            SerializedProperty OutputFromIndex(int i) => eventsListProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(TimedEvent.output));


            static void MakeHighlightable(VisualElement element)
            {
                Color color = element.style.color.value;
                Color backColor = element.style.backgroundColor.value;

                element.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    element.style.color = new Color(color.r + 0.2f, color.g + 0.2f, color.b + 0.2f);
                    element.style.backgroundColor = new Color(backColor.r + 0.2f, backColor.g + 0.2f, backColor.b + 0.2f);
                });
                element.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    element.style.color = color;
                    element.style.backgroundColor = backColor;
                });
            }
        }
#if ULT_EVENTS
        [ContextMenu("Convert Animation Events")]
        void ConvertAnimationEvents()
        {
            //Show popup to get AnimationClip Input from user.
            string path = EditorUtility.OpenFilePanel("Select Animation Clip", "Assets\\Actors\\_Private\\Angus\\src\\Animations", "anim");
            if (string.IsNullOrEmpty(path)) return;
            path = "Assets" + path.Substring(Application.dataPath.Length);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) return;

            // Get the Animation Events from the clip
            AnimationEvent[] animationEvents = AnimationUtility.GetAnimationEvents(clip);
            if (animationEvents.Length == 0) return;

            int i = 0;
            bool[] converted = new bool[animationEvents.Length];

            // Convert each Animation Event to a TimedEvent
            foreach (var animEvent in animationEvents)
            {
                TimedEvent timedEvent = new()
                {
                    time = animEvent.time,
                    output = new()
                };

                if (animEvent.functionName == "FireSignalBasic" || animEvent.functionName == "FinishAction")
                {
                    TryGetComponent(out SLS.StateMachineH.Signals.SignalNode signal);
                    timedEvent.output = signal[animEvent.functionName == "FireSignalBasic" ? animEvent.stringParameter : "Finish"];
                    signal.signals.Remove(animEvent.functionName == "FireSignalBasic" ? animEvent.stringParameter : "Finish");
                    converted[i] = true;
                }

                if (animEvent.functionName == "Lock" || animEvent.functionName == "Unlock" || animEvent.functionName == "ReadyNextAction")
                {
                    TryGetComponentFromMachine(out SLS.StateMachineH.Signals.SignalManager signalManager);

                    UltEvent.AddPersistentCall(ref timedEvent.output,
                        animEvent.functionName == "Lock" ? signalManager.Lock
                        : signalManager.Unlock);
                    converted[i] = true;
                }

                // Add the TimedEvent to the TimedEvents component
                events.Add(timedEvent);
                i++;
            }
            //Remove animation events that have been converted.
            List<AnimationEvent> remainingEvents = new();
            for (int j = 0; j < animationEvents.Length; j++)
                if (!converted[j])
                    remainingEvents.Add(animationEvents[j]);
            AnimationUtility.SetAnimationEvents(clip, remainingEvents.ToArray());
        }
#endif
#endif
    }
}