using SLS.StateMachineH;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static UnityEditor.Progress;

public class StateAnimatorUtils : MonoBehaviour
{
    public StateAnimator stateAnimator;
    public PlayerMovementAnimator playerMovementAnimator;
    public Animator animator;
    public AnimationClip animationClip;

    [ContextMenu("Get Stuff")]
    public void GetStuff()
    {
        TryGetComponent(out stateAnimator);
        if (stateAnimator == null) return;
        TryGetComponent(out playerMovementAnimator);
        stateAnimator.Machine.TryGetComponent(out animator);
        if (animator == null) return;

        string animationName = stateAnimator.name;

        RuntimeAnimatorController runtime = animator.runtimeAnimatorController;
        var animatorController = runtime as AnimatorController;

        foreach (ChildAnimatorState state in animatorController.layers[0].stateMachine.states)
        {
            if(state.state.name == animationName)
            {
                animationClip = state.state.motion as AnimationClip;
                break;
            }
        }
    }

    private struct AnimationCurveTransferer
    {
        [SerializeField]
        public AnimationClip clip;
        public AnimationCurve outputCurve;
        public EditorCurveBinding? binding;

        public string name;

        public AnimationCurveTransferer(string name, AnimationClip clip)
        {
            this.name = name;
            this.clip = clip;
            outputCurve = new AnimationCurve();
            binding = null;
        }

        public void FoundBinding(EditorCurveBinding binding)
        {
            this.binding = binding;
            Debug.Log($"Found Binding for {name}.");
            outputCurve = AnimationUtility.GetEditorCurve(clip, binding);
        }
        public void NoBinding(PlayerMovementAnimator blankSource)
        {
            if (blankSource == null) return;

            var type = typeof(PlayerMovementAnimator);
            var field = type.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                float value = (float)field.GetValue(blankSource);
                Debug.Log($"No Binding found for {name}. Using value from Script which is {value}.");
                outputCurve = AnimationCurve.Constant(0f, clip.length, value);
            }
        }

    }

    [ContextMenu("Recast")]
    public void Recast()
    {
        if (animationClip == null) return;
        AnimationClip clip = animationClip;

        var bindings =  AnimationUtility.GetCurveBindings(clip);

        var transferers = new AnimationCurveTransferer[]
        {
            new("influence", clip),
            new("maxSpeed", clip),
            new("minSpeed", clip),
            new("speedChangeRate", clip),
            new("turnability", clip),
            new("verticalAddSpeed", clip),
            new("terminalVelocity", clip),
            new("setVerticalInfluence", clip),
            new("setVerticalVelocity", clip),
            new("defaultGravity", clip),
            new("worldspaceInfluence", clip)
        };

        foreach (var item in transferers)
        {
            EditorCurveBinding? bindingFound = null;
            foreach (var binding in bindings)
            {
                if(binding.propertyName == item.name)
                {
                    bindingFound = binding;
                    break;
                }  
            }

            if (bindingFound.HasValue) item.FoundBinding(bindingFound.Value);
            else item.NoBinding(playerMovementAnimator);
            
            clip.SetCurve("", typeof(PlayerMovementAnimator), item.name, item.outputCurve);
            EditorUtility.SetDirty(clip);
        }
    }


    public AnimationCurve Curve;
}
