using Cinemachine;
using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MiscHelperMethods
{
    public static class PlayerMovementAnimatorTransferToRoots
    {
        public static void Basic(PlayerMovementAnimator THIS)
        {
            THIS.Machine.TryGetComponent(out Animator animator);
            if (animator == null) return;

            string animationName = THIS.intendedAnimationName;
            Debug.Log($"Beginning Recast State={THIS.State.name}, Animation={animationName}");

            AnimationClip animationClip = null;
            RuntimeAnimatorController runtime = animator.runtimeAnimatorController;
            var animatorController = runtime as AnimatorController;

            foreach (ChildAnimatorState state in animatorController.layers[0].stateMachine.states)
            {
                if (state.state.name == animationName)
                {
                    animationClip = state.state.motion as AnimationClip;
                    break;
                }
            }



            if (animationClip == null) return;
            AnimationClip clip = animationClip;

            var bindings = AnimationUtility.GetCurveBindings(clip);

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
                    if (binding.propertyName == item.name)
                    {
                        bindingFound = binding;
                        break;
                    }
                }

                if (bindingFound.HasValue) item.FoundBinding(bindingFound.Value);
                else item.NoBinding(THIS);

                clip.SetCurve("", typeof(PlayerMovementAnimator), item.name, item.outputCurve);
                EditorUtility.SetDirty(clip);
            }
        }

        public static void Conditional(PlayerMovementAnimatorConditional THIS)
        {
            THIS.Machine.TryGetComponent(out Animator animator);
            if (animator == null) return;

            string animationName = THIS.intendedAnimationName;
            Debug.Log($"Beginning Recast State={THIS.State.name}, Animation={animationName}");

            AnimationClip animationClip = null;
            RuntimeAnimatorController runtime = animator.runtimeAnimatorController;
            var animatorController = runtime as AnimatorController;

            foreach (ChildAnimatorState state in animatorController.layers[0].stateMachine.states)
            {
                if (state.state.name == animationName)
                {
                    animationClip = state.state.motion as AnimationClip;
                    break;
                }
            }



            if (animationClip == null) return;
            AnimationClip clip = animationClip;

            var bindings = AnimationUtility.GetCurveBindings(clip);

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
                    if (binding.propertyName == item.name)
                    {
                        bindingFound = binding;
                        break;
                    }
                }

                if (bindingFound.HasValue) item.FoundBinding(bindingFound.Value);
                else item.NoBinding(THIS);

                clip.SetCurve("", typeof(PlayerMovementAnimator), item.name, item.outputCurve);
                EditorUtility.SetDirty(clip);
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
                //Debug.Log($"Found Binding for {name}.");
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
                    //Debug.Log($"No Binding found for {name}. Using value from Script which is {value}.");
                    outputCurve = new AnimationCurve(new Keyframe(0, value));
                    //outputCurve = AnimationCurve.Constant(0f, clip.length, value);
                }
            }

        }
    }

    [MenuItem("Rage Rooster Tooling/Open Player Prefab")]
    public static void OpenPlayerPrefab() => 
        AssetDatabase.OpenAsset(
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Actors/_Private/Angus/Player.prefab")
            );



























}
