#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Cinemachine.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// PSEUDOCODE / PLAN (detailed):
// 1. Validate input clip is not null.
// 2. Begin an undo record for the clip so changes can be reverted via Unity editor.
// 3. Define epsilons for comparing times and float values.
// 4. Iterate float animation curves obtained via AnimationUtility.GetCurveBindings.
//    a. Skip null curves.
//    b. If curve has zero keys -> remove curve.
//    c. If all keyframe values are equal (within epsilon) -> remove curve.
//    d. Else if there are only up to two keyframes and those keyframe times are at the clip start and clip end (within epsilon) -> remove curve.
// 5. Iterate object reference curves obtained via AnimationUtility.GetObjectReferenceCurveBindings.
//    a. If no keys -> remove.
//    b. If all keyframe object references are equal -> remove.
//    c. Else if there are only up to two keyframes at start/end -> remove.
// 6. Mark clip dirty and save assets, log removal summary.
// 7. Keep changes minimal and use AnimationUtility.SetEditorCurve / SetObjectReferenceCurve to remove channels.
//
// NOTES:
// - This uses conservative heuristics because "default value" is not reliably queryable from the clip alone.
// - Epsilon values tuned to typical clip lengths; adjust if necessary.

public class AnimationHelperWindow : EditorWindow
{
    [MenuItem("Rage Rooster Tooling/Animation Helper Window")]
    public static new void Show()
    {
        AnimationHelperWindow w = ScriptableObject.CreateInstance<AnimationHelperWindow>();
        w.titleContent = new("Debug Tools Window");
        w.ShowUtility();
    }

    //private List<AnimationClip> manyAnimationClips = new();

    //ObjectField InputAnimationClipField;
    //SerializedProperty InputManyAnimationClipProp;
    //PropertyField InputManyAnimationClipField;
    Button ClearUnecessaryChannelsButton;

    private void OnEnable()
    {
        //InputAnimationClipField = new("Animation Clip")
        //{
        //    objectType = typeof(AnimationClip),
        //};
        ClearUnecessaryChannelsButton = new Button(ClearUnecessaryChannelsButtonPress)
        {
            text = "Clear Unecessary Channels"
        };
        //rootVisualElement.Add(InputAnimationClipField);
        rootVisualElement.Add(ClearUnecessaryChannelsButton);

        //InputManyAnimationClipProp = new SerializedObject(this).FindProperty(nameof(manyAnimationClips));
        //InputManyAnimationClipField = new(InputManyAnimationClipProp);
        //rootVisualElement.Add(InputManyAnimationClipField); 

    }

    public void ClearUnecessaryChannelsButtonPress()
    {
        for (int i = 0; i < Selection.objects.Length; i++)
        {
            if (Selection.objects[i] is AnimationClip c) ClearUnnecessaryChannels(c);
        }


        //if (InputManyAnimationClipProp.arraySize > 0)
        //    for (int i = 0; i < InputManyAnimationClipProp.arraySize; i++)
        //        ClearUnnecessaryChannels(InputManyAnimationClipProp.GetArrayElementAtIndex(i).objectReferenceValue as AnimationClip);
        //else ClearUnnecessaryChannels(InputAnimationClipField.value as AnimationClip);
    }

    public void ClearUnnecessaryChannels(AnimationClip source)
    {
        if (source == null)
        {
            Debug.LogWarning("No AnimationClip provided to ClearUnnecessaryChannels.");
            return;
        }

        // Record undo so user can revert.
        Undo.RecordObject(source, "Clear Unnecessary Animation Channels");

        const float timeEpsilon = 0.001f;
        const float valueEpsilon = 0.0001f;

        int removedCount = 0;
        float clipLength = Mathf.Max(0f, source.length);

        // Float/curve bindings
        var floatBindings = AnimationUtility.GetCurveBindings(source);
        foreach (var binding in floatBindings)
        {
            var curve = AnimationUtility.GetEditorCurve(source, binding);
            if (curve == null)
                continue;

            var keys = curve.keys;
            if (keys == null || keys.Length == 0)
            {
                AnimationUtility.SetEditorCurve(source, binding, null);
                removedCount++;
                continue;
            }

            // If all key values are effectively identical -> remove channel
            bool allSameValue = true;
            float firstVal = keys[0].value;
            for (int i = 1; i < keys.Length; i++)
            {
                if (Mathf.Abs(keys[i].value - firstVal) > valueEpsilon)
                {
                    allSameValue = false;
                    break;
                }
            }

            if (allSameValue)
            {
                AnimationUtility.SetEditorCurve(source, binding, null);
                removedCount++;
                continue;
            }

            // If only keys are at the very beginning and very end (<=2 keys) -> remove channel
            if (keys.Length <= 2)
            {
                float firstTime = keys[0].time;
                float lastTime = keys[keys.Length - 1].time;
                bool firstAtStart = Mathf.Abs(firstTime - 0f) <= timeEpsilon;
                bool lastAtEnd = Mathf.Abs(lastTime - clipLength) <= timeEpsilon;
                if (firstAtStart && lastAtEnd)
                {
                    AnimationUtility.SetEditorCurve(source, binding, null);
                    removedCount++;
                    continue;
                }
            }
        }

        // Object reference bindings (e.g., sprite swaps)
        var objBindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
        foreach (var binding in objBindings)
        {
            var keyframes = AnimationUtility.GetObjectReferenceCurve(source, binding);
            if (keyframes == null || keyframes.Length == 0)
            {
                AnimationUtility.SetObjectReferenceCurve(source, binding, null);
                removedCount++;
                continue;
            }

            bool allSameRef = true;
            var firstRef = keyframes[0].value;
            for (int i = 1; i < keyframes.Length; i++)
            {
                if (keyframes[i].value != firstRef)
                {
                    allSameRef = false;
                    break;
                }
            }

            if (allSameRef)
            {
                AnimationUtility.SetObjectReferenceCurve(source, binding, null);
                removedCount++;
                continue;
            }

            if (keyframes.Length <= 2)
            {
                float firstTime = keyframes[0].time;
                float lastTime = keyframes[keyframes.Length - 1].time;
                bool firstAtStart = Mathf.Abs(firstTime - 0f) <= timeEpsilon;
                bool lastAtEnd = Mathf.Abs(lastTime - clipLength) <= timeEpsilon;
                if (firstAtStart && lastAtEnd)
                {
                    AnimationUtility.SetObjectReferenceCurve(source, binding, null);
                    removedCount++;
                    continue;
                }
            }
        }

        EditorUtility.SetDirty(source);
        try
        {
            AssetDatabase.SaveAssets();
        }
        catch
        {
            // Ignored - saving assets may fail in some contexts; clip changes still applied in memory.
        }

        Debug.Log($"ClearUnnecessaryChannels: Removed {removedCount} channel(s) from clip '{source.name}'.");
    }


}
#endif