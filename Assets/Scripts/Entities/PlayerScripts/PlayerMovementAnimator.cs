using EditorAttributes;
using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class PlayerMovementAnimator : PlayerMovementEffector
{
    [Tooltip("Generally recommended to keep at 0 and have set to 1 in animation so that the CrossFade can automatically smoothly blend the effect."), Range(0,1)]
    public float influence;
    public bool fullStop;

    public float maxSpeed = 0;
    public float minSpeed = 0;
    public float speedChangeRate = 15;
    public float turnability = 10;
    public float verticalAddSpeed;
    public float terminalVelocity = 98.1f;

    [Tooltip("Sets/Lerps the velocity to a specific point rather than adding it.")]
    [Range(0, 1)] public float setVerticalInfluence;
    public float setVerticalVelocity;
    [Tooltip("Only active if locked.")]
    public float defaultGravity;

    [Range(0, 1)] public float worldspaceInfluence;
    public Vector3 worldspaceVelocity;

    [Tooltip("Makes this Movement Effector inoperable no matter the parameters. Must be set by some kind of alternative source, or by an inheriting class.")]
    public bool locked;

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        if(locked)
        {
            base.HorizontalMovement(out resultX, out resultZ);
            return;
        }

        resultX = playerMovementBody.velocity.x;
        resultZ = playerMovementBody.velocity.z;

        if (influence > 0)
        {
            Vector3 controlVector = playerController.camAdjustedMovement;

            Vector3 targetDirection = playerMovementBody.direction;
            float targetSpeed = playerMovementBody.CurrentSpeed;

            if (turnability > 0) targetDirection = Vector3.RotateTowards(targetDirection, controlVector.normalized, turnability * Mathf.PI * Time.fixedDeltaTime, 0);

            targetSpeed = controlVector.sqrMagnitude > 0
                ? targetSpeed.MoveTowards(controlVector.magnitude * speedChangeRate * (Time.deltaTime * 50), maxSpeed)
                : targetSpeed.MoveTowards(speedChangeRate * (Time.deltaTime * 50), minSpeed);

            if (influence == 1)
            {
                playerMovementBody.CurrentSpeed = targetSpeed;
                playerMovementBody.InstantDirectionChange(targetDirection);
                resultX = targetDirection.x * targetSpeed;
                resultZ = targetDirection.z * targetSpeed;
            }
            else
            {
                playerMovementBody.CurrentSpeed = Mathf.Lerp(playerMovementBody.CurrentSpeed, targetSpeed, influence);
                playerMovementBody.InstantDirectionChange(Vector3.Lerp(playerMovementBody.direction, targetDirection, influence));
                resultX = Mathf.Lerp(resultX.Value, targetDirection.x * targetSpeed, influence);
                resultZ = Mathf.Lerp(resultZ.Value, targetDirection.z * targetSpeed, influence);
            }

        }
        if (worldspaceInfluence > 0)
        {
            Vector3 relative = transform.TransformDirection(worldspaceVelocity);
            resultX = worldspaceInfluence == 1
                ? relative.x
                : Mathf.Lerp(resultX.Value, relative.x, worldspaceInfluence);
            resultZ = worldspaceInfluence == 1
                ? relative.z
                : Mathf.Lerp(resultZ.Value, relative.z, worldspaceInfluence);
        }
        if (fullStop)
        {
            resultX = 0;
            resultZ = 0;
        }

    }
    public override void VerticalMovement(out float? result)
    {
        if (locked)
        {
            result = playerMovementBody.velocity.y - defaultGravity * .02f;
            return;
        }

        result = playerMovementBody.velocity.y;

        if (!Mathf.Approximately(verticalAddSpeed, 0)) result = (result.Value + verticalAddSpeed * Time.fixedDeltaTime).Min(-terminalVelocity);
        if(setVerticalInfluence > 0) 
            result = setVerticalInfluence == 1 
                ? setVerticalVelocity 
                : Mathf.Lerp(result.Value, setVerticalVelocity, setVerticalInfluence);
        if (worldspaceInfluence > 0)
        {
            result = worldspaceInfluence == 1 
                ? worldspaceVelocity.y 
                : Mathf.Lerp(result.Value, worldspaceVelocity.y, worldspaceInfluence);
        }
        if (fullStop)
        {
            result = 0;
        }
    }

    protected override void OnExit(State next)
    {
        locked = false;
    }






    [ContextMenu("Recast")]
    private void RunTransfer()
    {
        TryGetComponent(out StateAnimator stateAnimator);
        if (stateAnimator == null) return;
        stateAnimator.Machine.TryGetComponent(out Animator animator);
        if (animator == null) return;

        string animationName = stateAnimator.name;

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
            else item.NoBinding(this);

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
}
