using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using Utilities.Xtensions.VisualElements;
using EPOOutline;





#if UNITY_EDITOR
using UnityEditor;
#endif

public class Target : MonoBehaviour
{
    public Polymorph.UniqueList<TargetType> Types;

    [SerializeField] Vector3 RealPositionOffset;
    [SerializeField, RelatedComponent] Rigidbody rigidBody;
    [SerializeField, RelatedComponent] new Collider collider;
    [SerializeField, RelatedComponent] NavMeshAgent navMeshAgent;
    [SerializeField] CenterComputationType centerComputationType;
    [SerializeField, RelatedComponent] Outlinable outlinable;

    public enum CenterComputationType
    {
        Collider,
        Rigidbody,
        SetOffset
    }

    public Vector3 position => centerComputationType switch
    {
        CenterComputationType.Collider => collider.bounds.center,
        CenterComputationType.SetOffset => transform.position + transform.TransformVector(RealPositionOffset),
        CenterComputationType.Rigidbody => rigidBody.worldCenterOfMass,
        _ => transform.position
    };

    public float GetDistance(TargetingRange range) => Vector3.Distance(range.front.position, position);
    public float GetAngle(TargetingRange range) => Vector3.Angle(range.front.forward, position - range.front.position);

    private void OnEnable()
    {
        // Ensure each sub-component knows its owning Target in case the editor failed to serialize the reference
        for (int i = 0; i < Types.Count; i++)
        {
            if (Types[i] == null) continue;
            Types[i].Target = this;
            Types[i].Enabled = true;
        }
    }
    private void OnDisable()
    {
        for (int i = 0; i < Types.Count; i++)
        {
            if (Types[i] == null) continue;
            Types[i].Target = this; // keep reference available until fully torn down
            Types[i].Enabled = false;
        }
    }

    private void Awake()
    {
        // Runtime safety: assign owner to any serialized sub-components
        for (int i = 0; i < Types.Count; i++)
            if (Types[i] != null)
                Types[i].Target = this;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Editor-time: ensure the Target reference is present for each polymorph entry so editor drawers and tools
        // that expect the back-reference don't observe nulls.
        if (Types == null) return;
        for (int i = 0; i < Types.Count; i++)
            if (Types[i] != null)
                Types[i].Target = this;
    }
#endif

    public virtual Vector3 PredictFuturePosition(Vector3 projectileInitPos, float projectileSpeed)
    {

        Vector3 toTarget = position - projectileInitPos;
        float distanceToTarget = toTarget.magnitude;
        float timeToReachTarget = distanceToTarget / projectileSpeed;
        return position + (GetVelocity() * timeToReachTarget);

    }
    public virtual Vector3 GetVelocity()
    {
        if (rigidBody != null)
            return rigidBody.linearVelocity;
        if (navMeshAgent != null)
            return navMeshAgent.velocity;
        return Vector3.zero;
    }

    public virtual void Move(Vector3 offset)
    {

    }

    public TargetType this[System.Type T] => Types[T];
    public TargetState GetTargetState(System.Type T) => Types[T] != null ? Types[T].TargetState : TargetState.Inactive;

    public void UpdateTargetedState()
    {
        if (outlinable == null) return;
        int value = GetTargetState(typeof(TargetType.Interactable)) == TargetState.Targeted ? 3
            : GetTargetState(typeof(TargetType.Melee)) == TargetState.Targeted ? 2
            : GetTargetState(typeof(TargetType.Ranged)) == TargetState.Targeted ? 1
            : 0;
        outlinable.enabled = value != 0;
        outlinable.OutlineParameters.Color = value switch
        {
            3 => Color.green,
            2 => new(1, .627450980392156862f, 0),
            1 => Color.whiteSmoke,
            _ => Color.clear
        };
    }

}

[System.Serializable]
public abstract class TargetType : Polymorph
{
    public Target Target;

    public abstract TargetingChannel thisChannel { get; }

    public bool Enabled
    {
        get => currentState != TargetState.Inactive;
        set
        {
            currentState = value ? TargetState.OutOfRange : TargetState.Inactive;

            if (value && !thisChannel.AllTargets.Contains(this)) thisChannel.AllTargets.Add(this);
            if (!value && thisChannel.AllTargets.Contains(this)) thisChannel.AllTargets.Remove(this);
        }
    }

    public TargetState TargetState
    {
        get => currentState;
        set
        {
            if (currentState == value) return;
            if (currentState == TargetState.Inactive || value == TargetState.Inactive) return;

            if (currentState == TargetState.OutOfRange && value == TargetState.WithinRange)
                OnEnterRange();
            else if (currentState == TargetState.WithinRange && value == TargetState.OutOfRange)
                OnExitRange();

            currentState = value;
        }
    }
    protected TargetState currentState;

    public virtual void OnEnterRange() { }
    public virtual void OnExitRange() { }

    public virtual void OnDeTargeted(TargetType nextTarget) { }
    public virtual void OnTargeted(TargetType prevTarget) { }

    [System.Serializable]
    public class Melee : TargetType
    {
        public override TargetingChannel thisChannel => TargetingManager.MeleeChannel;
    }
    [System.Serializable]
    public class Ranged : TargetType
    {
        public override TargetingChannel thisChannel => TargetingManager.RangedChannel;

    }
    [System.Serializable]
    public class Interactable : TargetType
    {
        public override TargetingChannel thisChannel => TargetingManager.InteractionChannel;

        public static GameObject InteractionPopup;

        public Vector3 PopupPosition;
        public UltEvents.UltEvent OnInteract;
        public override void OnTargeted(TargetType prevTarget)
        {
            InteractionPopup.SetActive(true);
            InteractionPopup.transform.position = Target.transform.position + PopupPosition;
        }
        public override void OnDeTargeted(TargetType nextTarget)
        {
            if (!nextTarget) InteractionPopup.SetActive(false);
        }

    }

    public override bool OverrideBody(VisualElement container, SerializedProperty property)
    {
        SerializedProperty ThisProp = property.FindPropertyRelative(nameof(Target));
        ThisProp.objectReferenceValue = property.serializedObject.targetObject;

        container.DelayedBuild(() =>
        {
            PropertyField ThisField;
            if (container.QCache(out ThisField)) ThisField.style.display = DisplayStyle.None;
        });

        return false;
    }

    public static implicit operator bool(TargetType @in) => @in != null && @in.Enabled;
}

public enum TargetState
{
    Inactive,
    OutOfRange,
    WithinRange,
    Targeted
}
