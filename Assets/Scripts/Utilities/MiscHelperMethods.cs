using RageRooster.RoomSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
#endif

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

        /*
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
        */

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
        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Actors/_Private/Angus/Player.prefab"));

    [MenuItem("Rage Rooster Tooling/Open Gameplay Scene")]
    public static void OpenGameplayScene() => EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity");
        

    public static bool OnBeforeSerializationWasEditorCommonUpdate(out string name)
    {
        name = "";
#if UNITY_EDITOR
        name = new System.Diagnostics.StackTrace().GetFrame(2)?.GetMethod()?.Name;

        if (name == "Internal_VerifyModifiedMonoBehaviours"
            || name == "Update"
            || name == "RecordObject")
            return true;
#endif
        return false;
    }

























}

#if UNITY_EDITOR

public static class MiscHelperMethods_Editor
{
    public static SerializedProperty FindProperty(this SerializedProperty prop, string propertyName, bool backingField = false, string nestedPath = null)
    {
        string path =
            (string.IsNullOrEmpty(nestedPath) ? "" : nestedPath.EndsWith(".") ? nestedPath : nestedPath + ".")
            + (backingField ? "<" : "")
            + propertyName 
            + (backingField ? ">k__BackingField" : "");

        return prop.FindPropertyRelative(path);
    }
    public static SerializedProperty FindProperty(this SerializedObject obj, string propertyName, bool backingField = false, string nestedPath = null)
    {
        string path =
            (string.IsNullOrEmpty(nestedPath) ? "" : nestedPath.EndsWith(".") ? nestedPath : nestedPath + ".")
            + (backingField ? "<" : "")
            + propertyName
            + (backingField ? ">k__BackingField" : "");

        return obj.FindProperty(path);
    }

    /// <summary>
    /// Adds the surrounding <>k__BackingField to a property name, to reference the backing field of an auto-property.
    /// </summary>
    /// <param name="propertyName">the input property name. Generally advised to use a "nameof()"</param>
    /// <returns>the identifier of the backing field for use in a FindProperty method.</returns>
    public static string BackingField(this string propertyName) => $"<{propertyName}>k__BackingField";

}

#endif