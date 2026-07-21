using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ListUtilities.Editor;
using SLS.StateMachineH.Editor;
using SLS.StateMachineH.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


#if ULT_EVENTS
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

namespace SLS.StateMachineH.Signals
{
    [CustomPropertyDrawer(typeof(SignalSet))]
    internal class SignalSetDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement Display;
    
            Type DrawerType = typeof(HashedListDrawer.ListDrawer<>)
                .MakeGenericType(typeof(EVENT));
            var literal = fieldInfo.GetValue(property.serializedObject.targetObject)
                as ISerializedDictionaryNonGeneric;
    
            // Pass the live literal (the actual dictionary instance) to the drawer so it
            // can recalculate occurrences and provide proper binding. Using property.boxedValue
            // here returned a boxed/copy and left Literal null which caused blank/uneditable fields.
            Display = Activator.CreateInstance(DrawerType, property, literal, true) as VisualElement;
    
            return Display;
        }
    }

    
    [CustomEditor(typeof(SignalManager))]
    public class SignalManagerEditor : UnityEditor.Editor
    {
        Foldout activityDisplay;
        new SignalManager target;
        List<SignalVis> signals = new();

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();

            root.Add(new PropertyField(serializedObject.FindProperty("signals")));
            root.Add(new PropertyField(serializedObject.FindProperty("queueSignals")));

            activityDisplay = new Foldout();
            activityDisplay.text = "Activity Display";
            root.Add(activityDisplay);

            target = (SignalManager)base.target;
            target.onSignalQueueUpdate += QueueUpdate;
            target.onUpdate += TimeUpdate;

            return root;
        }
        private void OnDisable()
        {
            target.onSignalQueueUpdate -= QueueUpdate;
            target.onUpdate -= TimeUpdate;
        }

        public void QueueUpdate()
        {
            activityDisplay.Clear();
            signals.Clear();
            bool first = true;
            foreach (Signal signal in target.SignalQueue)
            {
                signals.Add(new SignalVis(signal, first));
                activityDisplay.Add(signals[^1]);
                signals[^1].Update(0);
                first = false;
            }
                
        }
        public void TimeUpdate() => signals[0].Update(target.SignalQueueTimer);

        public class SignalVis : VisualElement
        {
            float endTime;
            
            Label timerText;
            VisualElement meterBack;
            VisualElement meterFill;

            public SignalVis(Signal s, bool first)
            {
                style.flexDirection = FlexDirection.Row;
                Label label = new(s.name); this.Add(label);
                label.style.width = Length.Percent(30);

                string notesS = "";
                if (s.ignoreLock) notesS += "L";
                if (s.allowDuplicates) notesS += "D";
                Label notes = new(notesS); this.Add(notes);
                notes.style.width = 18;

                if (first)
                {
                    endTime = s.queueTime;

                    // Create meter background container (relative) so fill can be absolute inside it.
                    meterBack = new VisualElement()
                    {
                        style =
                        {
                            position = Position.Relative,
                            flexGrow = 1,
                            height = 12,
                            marginLeft = 4,
                            marginRight = 4,
                            alignSelf = Align.Center,
                            backgroundColor = new Color(0f, 0f, 0f, 0.12f),
                        }
                    };
                    this.Add(meterBack);

                    // Create the actual fill bar (absolute positioned)
                    meterFill = new VisualElement()
                    {
                        style =
                        {
                            position = Position.Absolute,
                            left = 0,
                            top = 0,
                            bottom = 0,
                            width = Length.Percent(0),
                            backgroundColor = new Color(0.2f, 0.6f, 1f, 0.35f),
                        }
                    };
                    meterBack.Add(meterFill);

                    // Timer text sits above the fill (higher z-index)
                    timerText = new Label(s.queueTime.ToString())
                    {
                        style =
                        {
                            position = Position.Relative,
                            unityTextAlign = TextAnchor.MiddleCenter,
                            alignSelf = Align.Center,
                            width = Length.Percent(100),
                            unityFontStyleAndWeight = FontStyle.Bold,
                        }
                    };
                    meterBack.Add(timerText);
                     
                }
            }

            public void Update(float time)
            {
                if (timerText != null) timerText.text = $"{time} / {endTime}";

                if (meterFill != null)
                {
                    float pct = Mathf.Clamp01((endTime - time) / endTime) * 100f;
                    meterFill.style.width = Length.Percent(pct);
                }
            }
        }
    }
}