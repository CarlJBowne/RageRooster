using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SLS.StateMachineH;
using SLS.StateMachineH.Signals;
using SLS.StateMachineH.Timelines;
using UltEvents;
using Utilities.Xtensions;

public static class EditorHelpers
{
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

        Handles.color = color.Changed(a: .12f);
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
        if (reference == null) return;
        DrawEditableFlatCone(reference.position, reference.forward, reference.up, distance, angle, color);
    }

    /*
    public static void TransferSignalsToTimelines()
    {
        //Get the StateMachine attached to the object selected in the editor

        StateMachine machine = Selection.gameObjects[0].GetComponent<StateMachine>();
        Animator machineAnimator = machine.GetComponent<Animator>();
        SignalManager signalManager = machine.GetComponent<SignalManager>();
        PlayerController playerController = machine.GetComponent<PlayerController>();

        void DoStateRecursive(State thisState)
        {
            thisState.TryGetComponent(out StateAnimator stateAnimator);

            if(stateAnimator != null)
            {
                //Get the animation clip associated with this state from the machineAnimator based on the stateAnimator's clip name

                AnimationClip clip = null;

                foreach (var clipInAnimator in machineAnimator.runtimeAnimatorController.animationClips)
                {
                    if (clipInAnimator.name == stateAnimator.name)
                    {
                        clip = clipInAnimator;
                        break;
                    }
                }

                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
                bool alreadyDone = false;
                SignalNode signalNode = null;
                TimedEvents timedEvents = null;

                for (int i = 0; i < events.Length; i++)
                {
                    //FireSingalBasic, Lock, Unlock, FinishAction
                    if (events[i].functionName == "FireSignalBasic")
                    {
                        FirstSuccess();
                        timedEvents.events.Add(new()
                        {
                            time = events[i].time,
                            output = signalNode[events[i].stringParameter]
                        });
                    }
                    else if (events[i].functionName == "Lock")
                    {
                        FirstSuccess();
                        UltEvent newEvent = new();
                        newEvent.AddPersistentCall(() =>
                        {
                            signalManager.Lock();
                        });
                        timedEvents.events.Add(new()
                        {
                            time = events[i].time,
                            output = newEvent
                        });
                    }
                    else if (events[i].functionName == "Unlock")
                    {
                        FirstSuccess();
                    }
                    else if (events[i].functionName == "FinishAction")
                    {
                        FirstSuccess();
                    }
                }
                void FirstSuccess()
                {
                    if(alreadyDone) return;
                    alreadyDone = true;
                    //Add a StateTimeline behavior to the state if it doesn't already have one
                    timedEvents = thisState.GetOrAddComponent<TimedEvents>();
                    signalNode = timedEvents.GetComponent<SignalNode>();
                }
            }

            EditorUtility.SetDirty(thisState.gameObject);

            foreach (var childState in thisState.Children) DoStateRecursive(childState);
        }
    }*/


    [MenuItem("Assets/ApplyAllPrefabOverridesRecursive")]
    public static void ApplyAllPrefabOverridesRecursive()
    {
        GameObject prefabGameObject = Selection.activeObject as GameObject;
        var status = PrefabUtility.GetPrefabInstanceStatus(prefabGameObject);
        if (status != PrefabInstanceStatus.NotAPrefab && status != PrefabInstanceStatus.MissingAsset)
        {
            var origFab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabGameObject);
            if (origFab != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(origFab);
                ApplyAllPrefabChangesInGivenHierarchyToPrefabAtPath(assetPath, prefabGameObject);
            }
        }
    }

    private static void ApplyAllPrefabChangesInGivenHierarchyToPrefabAtPath(string assetPath, GameObject hierarchy)
    {
        var status = PrefabUtility.GetPrefabInstanceStatus(hierarchy);
        if (status != PrefabInstanceStatus.NotAPrefab)
        {
            foreach (var ob in PrefabUtility.GetAddedComponents(hierarchy.gameObject)) ob.Apply(assetPath);
            foreach (var ob in PrefabUtility.GetObjectOverrides(hierarchy.gameObject)) ob.Apply(assetPath);
            foreach (var ob in PrefabUtility.GetAddedGameObjects(hierarchy.gameObject)) ob.Apply(assetPath);
            foreach (var ob in PrefabUtility.GetRemovedComponents(hierarchy.gameObject)) ob.Apply(assetPath);
        }
        for (int i = 0; i < hierarchy.transform.childCount; i++)
        {
            ApplyAllPrefabChangesInGivenHierarchyToPrefabAtPath(assetPath, hierarchy.transform.GetChild(i).gameObject);
        }
    }



}