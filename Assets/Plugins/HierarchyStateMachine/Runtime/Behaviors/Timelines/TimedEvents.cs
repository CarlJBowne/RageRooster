using SLS.StateMachineH.SerializedDictionary;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

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
            if(events.Count == 0) 
                Debug.LogWarning($"TimedEvents on State '{State.name}' in StateMachine '{Machine.name}' has no events configured and will not function.");
#endif
        }

        protected override void OnBegin()
        {
            elapsedTime = 0f;
            nextEventID = 0;
            if (events[0].time == 0)
            {
                events[0].output?.Invoke();
                nextEventID++;
            }
        }

        protected override void OnTick(float delta)
        {
            if(nextEventID < events.Count && WasPointPassed(events[nextEventID].time))
            {
                events[nextEventID].output?.Invoke();
                nextEventID++;
                if(nextEventID >= events.Count && loopAfterLastEvent)
                {
                    elapsedTime %= events[^1].time;
                    nextEventID = 0;
                }
            }

        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(TimedEvents))]
        public class Editor : UnityEditor.Editor
        {
            protected SerializedProperty eventsListProperty;
            protected ReorderableList reorderableList;
            protected new TimedEvents target;

            private string noEventsHelpBoxText = "No timed events have been added. This system will not work without at least one. Click the + button to add one.";

            protected virtual void OnEnable()
           {
                eventsListProperty = serializedObject.FindProperty(nameof(TimedEvents.events));

                reorderableList = new(serializedObject, eventsListProperty);
                reorderableList.draggable = false;
                reorderableList.displayAdd = true;
                reorderableList.displayRemove = true;
                reorderableList.drawElementCallback = DrawElement;
                reorderableList.elementHeightCallback = ElementHeight;
                reorderableList.onAddCallback = AddElement;
                reorderableList.onRemoveCallback = RemoveElement;
                target = (TimedEvents)base.target;
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                reorderableList.DoLayoutList();
                if(eventsListProperty.arraySize == 0) EditorGUILayout.HelpBox(noEventsHelpBoxText, MessageType.Info);
                target.loopAfterLastEvent = EditorGUILayout.Toggle("Loop After Last Event", target.loopAfterLastEvent);
                serializedObject.ApplyModifiedProperties();
            }

            void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                SerializedProperty timeProp = element.FindPropertyRelative(nameof(TimedEvent.time));
                SerializedProperty outputProp = element.FindPropertyRelative(nameof(TimedEvent.output));
                Rect timeRect = new(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                Rect outputRect = new(rect.x, rect.y + EditorGUIUtility.singleLineHeight, rect.width, EditorGUI.GetPropertyHeight(outputProp));
                EditorGUI.BeginChangeCheck();
                EditorGUI.DelayedFloatField(timeRect, timeProp);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    ReorderElements(index);
                }
                EditorGUI.PropertyField(outputRect, outputProp);
            }
            float ElementHeight(int index)
            {
                SerializedProperty outputProp = reorderableList
                    .serializedProperty
                    .GetArrayElementAtIndex(index)
                    .FindPropertyRelative(nameof(TimedEvent.output));
                return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(outputProp);
            }
            void AddElement(ReorderableList list)
            {
                eventsListProperty.arraySize++;
                SerializedProperty newElement = eventsListProperty.GetArrayElementAtIndex(eventsListProperty.arraySize - 1);
                SerializedProperty timeProp = newElement.FindPropertyRelative(nameof(TimedEvent.time));
                SerializedProperty outputProp = newElement.FindPropertyRelative(nameof(TimedEvent.output));
                timeProp.floatValue = eventsListProperty.arraySize > 1 ?
                    eventsListProperty.GetArrayElementAtIndex(eventsListProperty.arraySize - 2).FindPropertyRelative(nameof(TimedEvent.time)).floatValue + .0005f
                    : 0f;
                //outputProp.managedReferenceValue = default;
            }
            void RemoveElement(ReorderableList list)
            {
                if (list.index < 0 || list.index >= eventsListProperty.arraySize) return;
                eventsListProperty.DeleteArrayElementAtIndex(list.index);
            }
            void ReorderElements(int index)
            {
                float thisTime = TimeFromIndex(index).floatValue;
                {
                    float prevTime = index > 0 ? TimeFromIndex(index - 1).floatValue : -199999999;
                    float nextTime = index < eventsListProperty.arraySize - 1 ? TimeFromIndex(index + 1).floatValue : 199999999;

                    if (thisTime > prevTime && thisTime < nextTime) return;
                }

                int i = 0;
                for (; i < eventsListProperty.arraySize; i++)
                    if(thisTime < TimeFromIndex(i).floatValue) 
                        break;

                eventsListProperty.MoveArrayElement(index, i-1);

                serializedObject.ApplyModifiedProperties();
            }

            SerializedProperty TimeFromIndex(int i) => eventsListProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(TimedEvent.time));
            SerializedProperty OutputFromIndex(int i) => eventsListProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(TimedEvent.output));




        }

#if ULT_EVENTS
        [ContextMenu("Convert Animation Events")]
        void ConvertAnimationEvents()
        {
            //Show popup to get AnimationClip Input from user.
            string path = UnityEditor.EditorUtility.OpenFilePanel("Select Animation Clip", "Assets\\Actors\\_Private\\Angus\\src\\Animations", "anim");
            if (string.IsNullOrEmpty(path)) return;
            path = "Assets" + path.Substring(Application.dataPath.Length);
            AnimationClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) return;

            // Get the Animation Events from the clip
            AnimationEvent[] animationEvents = AnimationUtility.GetAnimationEvents(clip);
            if (animationEvents.Length == 0) return;

            int i = 0;
            bool[] converted = new bool[animationEvents.Length];

            // Convert each Animation Event to a TimedEvent
            foreach (var animEvent in animationEvents)
            {
                TimedEvent timedEvent = new TimedEvent
                {
                    time = animEvent.time,
                    output = new()
                };

                if(animEvent.functionName == "FireSignalBasic" || animEvent.functionName == "FinishAction")
                {
                    TryGetComponent(out SLS.StateMachineH.Signals.SignalNode signal);
                    timedEvent.output = signal[animEvent.functionName == "FireSignalBasic" ? animEvent.stringParameter : "Finish"];
                    signal.signals.Remove(animEvent.functionName == "FireSignalBasic" ? animEvent.stringParameter : "Finish");
                    converted[i] = true;
                }

                if(animEvent.functionName == "Lock" || animEvent.functionName == "Unlock" || animEvent.functionName == "ReadyNextAction")
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