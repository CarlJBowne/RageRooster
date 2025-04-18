using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using static UnityEngine.Rendering.DebugUI;

public class RagdollHandler : Grabbable
{
    public float minRagdollTime;
    public float maxRagdollTime;
    public float minRagdollVelovity;
    private float ragDollTimer;

    public Collider nonRagdolledCollider;
    public Rigidbody nonRagdolledRigidBody;
    public int defaultLayer = Layers.Enemy;

    public bool advanced;
    public Collider[] ragDollColliders;
    public Rigidbody[] ragDollRigidBodies;

    private new Collider collider => advanced ? ragDollColliders[0] : nonRagdolledCollider;
    public override Rigidbody rigidBody => advanced ? ragDollRigidBodies[0] : nonRagdolledRigidBody;
    [SerializeField] private RagdollInteractionProxy proxy;
    public bool isPlayer;
    private Vector3[] savedLocalPos;

    public override bool IsGrabbable => base.IsGrabbable && !isPlayer;

    protected override void Awake()
    {
        
        health = GetComponent<EnemyHealth>();
        SetState(EntityState.Default);
        if(proxy) proxy.SetRagdoll(false);
        if (isPlayer)
        {
            savedLocalPos = new Vector3[ragDollColliders.Length];
            for (int i = 0; i < savedLocalPos.Length; i++)
                savedLocalPos[i] = ragDollColliders[i].transform.localPosition;
        }
    }
    private void FixedUpdate()
    {
        if (currentState == EntityState.RagDoll && maxRagdollTime > 0)
        {
            ragDollTimer += Time.deltaTime;
            if (ragDollTimer > minRagdollTime && (rigidBody.velocity.magnitude < minRagdollVelovity || ragDollTimer > maxRagdollTime))
                health.Destroy();
        }
        if(currentState is EntityState.Thrown or EntityState.RagDoll && advanced)
        {
            transform.position = ragDollRigidBodies[0].transform.position;
            ragDollRigidBodies[0].transform.localPosition = Vector3.zero;
        }
    }

    public override void SetState(EntityState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        GrabStateEvent?.Invoke(currentState);

        switch (newState)
        {
            case EntityState.Default:
                SetRagdoll(false);
                ragDollTimer = 0;
                nonRagdolledCollider.gameObject.layer = Layers.Enemy;
                break;
            case EntityState.Grabbed:
                SetRagdoll(true);
                if (advanced) ragDollColliders[0].transform.Reset(scale: false);
                if (proxy) proxy.transform.parent.Reset(scale: false);
                rigidBody.isKinematic = true;
                break;
            case EntityState.Thrown:
                SetRagdoll(true);
                break;
            case EntityState.RagDoll:
                SetRagdoll(true);
                ragDollTimer = 0;
                if (isPlayer)
                    for (int i = 0; i < savedLocalPos.Length; i++)
                        ragDollColliders[i].transform.localPosition = savedLocalPos[i];
                break;
            default:
                break;
        }

        (newState switch
        {
            EntityState.Grabbed => grabbedEvent,
            EntityState.Thrown => thrownEvent,
            EntityState.RagDoll => bounceEvent,
            _ => defaultEvent,
        })?.Invoke();
    }


    private void SetRagdoll(bool value)
    {
        if (advanced)
        {
            for (int i = 0; i < ragDollColliders.Length; i++)
            {
                ragDollColliders[i].enabled = value;
                ragDollRigidBodies[i].isKinematic = !value;
            }
        }

        if(nonRagdolledCollider)
        {
            nonRagdolledCollider.enabled = !value;
            nonRagdolledCollider.gameObject.layer = value ? Layers.NonSolid : defaultLayer;  
        }
        if(nonRagdolledRigidBody) nonRagdolledRigidBody.isKinematic = value;

        if(proxy) proxy.SetRagdoll(value);
    } 
    public override void SetVelocity(Vector3 globalVelocity)
    { 
        if (advanced)
            for (int i = 0; i < ragDollRigidBodies.Length; i++)
                ragDollRigidBodies[i].velocity = globalVelocity;
        else nonRagdolledRigidBody.velocity = globalVelocity;
    }
    public override void IgnoreCollisionWithThrower(bool ignore = true)
    {
        if (advanced)
        {
            for (int i = 0; i < ragDollColliders.Length; i++)
                Physics.IgnoreCollision(ragDollColliders[i], Grabber.ownerCollider, ignore);
            if (proxy) proxy.IgnoreCollisionWithThrower(Grabber.ownerCollider, ignore);
        }
        else Physics.IgnoreCollision(nonRagdolledCollider, Grabber.ownerCollider, ignore);
        
    }

}
