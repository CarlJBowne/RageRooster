using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SLS.StateMachineH
{
    [CustomEditor(typeof(SignalManager))]
    public class SignalManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SignalManager signalManager = (SignalManager)target;

            // Display serialized properties  
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            Rect totalArea = EditorGUILayout.GetControlRect();
            GUIStyle skin = GUI.skin.label;

            StateMachine_EditorUtilities.DrawScriptClicker<SignalManager>(signalManager, totalArea);

            Rect globalSignalsArea = totalArea;
            globalSignalsArea.y += EditorGUIUtility.singleLineHeight + 4;
            SerializedProperty globalSignals = serializedObject.FindProperty("globalSignals");
            globalSignalsArea.height = EditorGUI.GetPropertyHeight(globalSignals);
            EditorGUI.PropertyField(globalSignalsArea, globalSignals, new GUIContent("Global Signals"), true);

            Rect signalQueueArea = globalSignalsArea;
            signalQueueArea.y = signalQueueArea.yMax + 5;
            signalQueueArea.height = EditorGUIUtility.singleLineHeight;
            Rect headerSection = signalQueueArea;

            if (signalManager.queueSignals)
            {
                Queue<Signal> signalQueue = signalManager.SignalQueue;
                float signalQueueTimer = signalManager.SignalQueueTimer;
                float activeSignalLength = signalManager.ActiveSignalLength;


#if DEBUG_1
                {
                    signalQueue = new();
                    signalQueue.Enqueue(new Signal("Signal1", 1f));
                    signalQueue.Enqueue(new Signal("Signal2", 1f));
                    signalQueue.Enqueue(new Signal("Signal3", 1f));
                    signalQueueTimer = 0.45f;
                    activeSignalLength = 1f;
                }
#endif

                signalQueueArea.height = 5 + EditorGUIUtility.singleLineHeight * (signalQueue.Count + 1);
                GUI.Box(signalQueueArea, GUIContent.none, EditorStyles.helpBox);

                if (signalQueue != null && signalQueue.Count > 0)
                {
                    int i = 0;
                    foreach (var signal in signalQueue)
                    {
                        Rect signalRect = new Rect(
                            signalQueueArea.x + 5,
                            signalQueueArea.y + EditorGUIUtility.singleLineHeight * (i + 1),
                            signalQueueArea.width+10,
                            EditorGUIUtility.singleLineHeight);
                        if (i == 0)
                        {
                            EditorGUI.LabelField(signalRect, signal.name);

                            signalRect.x = signalRect.width * .5f;
                            signalRect.width = signalRect.width * .5f;
                            EditorGUI.ProgressBar(signalRect, signalQueueTimer / activeSignalLength, $"{signalQueueTimer:F2}s / {activeSignalLength:F2}s");
                        }
                        else
                        {
                            EditorGUI.LabelField(signalRect, signal.name);
                        }
                        i++;
                    }
                }
            }
            else
            {
                signalQueueArea.height = EditorGUIUtility.singleLineHeight;
                GUI.Box(signalQueueArea, GUIContent.none, EditorStyles.helpBox);
            }

            headerSection.x += 2;
            headerSection.width -= 14;
            signalManager.queueSignals = EditorGUI.ToggleLeft(headerSection, "", signalManager.queueSignals);

            
            skin.alignment = TextAnchor.MiddleCenter;
            skin.fontStyle = FontStyle.Bold;
            EditorGUI.LabelField(headerSection, "Signal Queue", skin);

            totalArea.height = signalQueueArea.yMax;
            GUILayout.Space(totalArea.height-15);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

}