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
        List<SignalVis> signals;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();

            root.Add(new PropertyField(serializedObject.FindProperty("signals")));

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
            bool first = true;
            foreach (Signal signal in target.SignalQueue)
            {
                signals.Add(new SignalVis(signal, first));
                activityDisplay.Add(signals[^1]);
                signals[^1].Update(target.SignalQueueTimer);
                first = false;
            }
                
        }
        public void TimeUpdate() => signals[0].Update(target.SignalQueueTimer);

        public class SignalVis : VisualElement
        {
            // Plan (pseudocode):
            // - Store endTime, timerText, meterBack, meterFill as fields.
            // - In constructor:
            //   - Create name label and notes label as before.
            //   - If this is the first (active) signal:
            //       - Record endTime from signal.queueTime.
            //       - Create a meter background element (meterBack) and style it to be relative.
            //       - Create a meter fill element (meterFill) positioned absolute, full height, left=0,
            //         width representing progress percent, and a semi-transparent color.
            //       - Create timerText label and ensure it has a higher z-index than meterFill so it
            //         renders on top (appearing "above" the meter).
            //       - Add meterBack (with meterFill) and timerText to this VisualElement.
            //   - Else (not first): create a simple timerText and add it (no meter).
            // - In Update(time):
            //   - Update timerText.text to show "time / endTime".
            //   - If meterFill exists, compute percentage = clamp(time / endTime, 0..1)*100 and set
            //     meterFill.style.width = Length.Percent(percentage).
            //   - Guard against endTime <= 0.
            //
            // Implementation follows using UIElements styles (Position, z-index, StyleColor, Length.Percent).

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
                notes.style.width = 15;

                if (first)
                {
                    endTime = s.queueTime;

                    // Create meter background container (relative) so fill can be absolute inside it.
                    meterBack = new VisualElement();
                    meterBack.style.position = Position.Relative;
                    meterBack.style.flexGrow = 1;
                    // set a reasonable height for the meter area
                    meterBack.style.minHeight = 18;
                    meterBack.style.marginLeft = 4;
                    meterBack.style.marginRight = 4;
                    meterBack.style.alignSelf = Align.Center;
                    meterBack.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.12f));

                    // Create the actual fill bar (absolute positioned)
                    meterFill = new VisualElement();
                    meterFill.style.position = Position.Absolute;
                    meterFill.style.left = 0;
                    meterFill.style.top = 0;
                    meterFill.style.bottom = 0;
                    meterFill.style.width = Length.Percent(0); // start empty
                    meterFill.style.backgroundColor = new StyleColor(new Color(0.2f, 0.6f, 1f, 0.35f));

                    meterBack.Add(meterFill);

                    // Timer text sits above the fill (higher z-index)
                    timerText = new Label(s.queueTime.ToString());
                    timerText.style.position = Position.Relative;
                    timerText.style.unityTextAlign = TextAnchor.MiddleCenter;
                    timerText.style.alignSelf = Align.Center;
                    timerText.style.width = Length.Percent(100);
                    timerText.style.unityFontStyleAndWeight = FontStyle.Bold;

                    // Add meter and timer to the row. Add meter first so it stretches,
                    // then timerText overlays it visually due to z-index.
                    this.Add(meterBack);
                    this.Add(timerText);
                }
                else
                {
                    timerText = new Label(s.queueTime.ToString());
                    this.Add(timerText);
                }
            }

            public void Update(float time)
            {
                if (timerText != null)
                {
                    timerText.text = $"{time} / {endTime}";
                }

                if (meterFill != null)
                {
                    float pct = 100f;
                    if (endTime > 0f)
                        pct = Mathf.Clamp01(time / endTime) * 100f;
                    pct = Mathf.Clamp(pct, 0f, 100f);
                    meterFill.style.width = Length.Percent(pct);
                }
            }
        }
    }
}