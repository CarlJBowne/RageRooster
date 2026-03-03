using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class NewTarget : MonoBehaviour
{
    public Polymorph.UniqueList<Behaviour> Behaviours;

    [SerializeField] Vector3 RealPositionOffset;
    [SerializeField, RelatedComponent] Rigidbody rigidBody;
    [SerializeField, RelatedComponent] new Collider collider;
    [SerializeField, RelatedComponent] NavMeshAgent navMeshAgent;
    [SerializeField] CenterComputationType centerComputationType;

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
        for (int i = 0; i < Behaviours.Count; i++) Behaviours[i]?.OnEnable();
    }
    private void OnDisable()
    {
        for (int i = 0; i < Behaviours.Count; i++) Behaviours[i]?.OnDisable();
    }

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

    public Behaviour this[Type T] => Behaviours[T];

    [System.Serializable]
    public abstract class Behaviour : Polymorph
    {
        public NewTarget This;
        //public abstract TargetingRange intendedRange { get; }

        public void OnEnable()
        {
            currentState = States.OutOfRange;
        }
        public void OnDisable()
        {
            currentState = States.Inactive;
        }

        public enum States
        {
            Inactive,
            OutOfRange,
            WithinRange,
            Targeted
        }
        public States TargetState
        {
            get => currentState;
            set
            {
                if (currentState == value) return;
                if (currentState == States.Inactive || value == States.Inactive) return;

                if (currentState == States.OutOfRange && value == States.WithinRange)
                    OnEnterRange();
                else if (currentState == States.WithinRange && value == States.OutOfRange)
                    OnExitRange();

                currentState = value;
            }
        }
        protected States currentState;

        public virtual void OnEnterRange()
        {

        }
        public virtual void OnExitRange()
        {

        }

        public virtual void OnDeTargeted(Target nextTarget) { }
        public virtual void OnTargeted(Target prevTarget) { }

        [System.Serializable]
        public class Melee : Behaviour
        {

        }
        [System.Serializable]
        public class Ranged : Behaviour
        {

        }
        [System.Serializable]
        public class Interactable : Behaviour
        {
            public Vector3 PopupPosition;
            public override void OnTargeted(Target prevTarget)
            {
                TargetingManager.InteractionPopup.SetActive(true);
                TargetingManager.InteractionPopup.transform.position = This.transform.position + PopupPosition;
            }
            public override void OnDeTargeted(Target nextTarget)
            {
                if (!nextTarget) TargetingManager.InteractionPopup.SetActive(false);
            }
        }

        public override bool OverrideBody(VisualElement.Hierarchy container, SerializedProperty property)
        {
            SerializedProperty ThisProp = property.FindPropertyRelative(nameof(This));
            ThisProp.objectReferenceValue = property.serializedObject.targetObject;
            PropertyField ThisField;
            if(container.parent.QCache(out ThisField)) ThisField.style.display = DisplayStyle.None;

            return false;
        }
    }
}

