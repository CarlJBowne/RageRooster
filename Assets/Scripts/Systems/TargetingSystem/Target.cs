using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Target : MonoBehaviour
{
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

    protected virtual void OnEnable()
    {
        if (!Gameplay.Active) return;
        TargetingManager.AddActiveTarget(this);
        currentState = States.OutOfRange;
    }
    protected virtual void OnDisable()
    {
        if (!Gameplay.Active) return;
        TargetingManager.RemoveActiveTarget(this);
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

    public virtual void OnEnterRange() { }
    public virtual void OnExitRange() { }

    public virtual void OnDeTargeted(Target nextTarget) { }
    public virtual void OnTargeted(Target prevTarget) { }

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


}
