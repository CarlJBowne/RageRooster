using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem.LowLevel;
using static UnityEngine.Rendering.DebugUI;

[Obsolete]
public class RagdollHandler_Obsolete : Grabbable_Obsolete
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

    public ColorTintAnimation materialTinter;


    protected override void Awake()
    {
        
        health = GetComponent<EnemyHealth>();
        State = EntityActivity.State.Default;
        if (proxy) proxy.SetRagdoll(false);
        if (isPlayer)
        {
            savedLocalPos = new Vector3[ragDollColliders.Length];
            for (int i = 0; i < savedLocalPos.Length; i++)
                savedLocalPos[i] = ragDollColliders[i].transform.localPosition;
        }
    }
    private void FixedUpdate()
    {
        if (currentState == EntityActivity.State.RagDoll && maxRagdollTime > 0)
        {
            ragDollTimer += Time.deltaTime;
            if (ragDollTimer > minRagdollTime && (rigidBody.linearVelocity.magnitude < minRagdollVelovity || ragDollTimer > maxRagdollTime))
                health.Destroy();
        }
        if(currentState is EntityActivity.State.Thrown or EntityActivity.State.RagDoll && advanced)
        {
            transform.position = ragDollRigidBodies[0].transform.position;
            ragDollRigidBodies[0].transform.localPosition = Vector3.zero;
        }
    }

    public override EntityActivity.State State
    {
        get => currentState;
        set
        {
            if (currentState == value) return;
            currentState = value;
            GrabStateEvent?.Invoke(currentState);

            switch (value)
            {
                case EntityActivity.State.Default:
                    SetRagdoll(false);
                    ragDollTimer = 0;
                    nonRagdolledCollider.gameObject.layer = Layers.Enemy;
                    break;
                case EntityActivity.State.Grabbed:
                    SetRagdoll(true);
                    if (advanced) ragDollColliders[0].transform.Reset(scale: false);
                    if (proxy) proxy.transform.parent.Reset(scale: false);
                    rigidBody.isKinematic = true;
                    break;
                case EntityActivity.State.Thrown:
                    SetRagdoll(true);
                    break;
                case EntityActivity.State.RagDoll:
                    SetRagdoll(true);
                    ragDollTimer = 0;
                    if (isPlayer)
                        for (int i = 0; i < savedLocalPos.Length; i++)
                            ragDollColliders[i].transform.localPosition = savedLocalPos[i];
                    break;
                default:
                    break;
            }

        (value switch
        {
            EntityActivity.State.Grabbed => grabbedEvent,
            EntityActivity.State.Thrown => thrownEvent,
            EntityActivity.State.RagDoll => bounceEvent,
            _ => defaultEvent,
        })?.Invoke();
        }
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
                ragDollRigidBodies[i].linearVelocity = globalVelocity;
        else nonRagdolledRigidBody.linearVelocity = globalVelocity;
    }

    public override void SetIgnoreCollision(Collider grabber, bool ignore = true)
    {
        if (advanced)
        {
            for (int i = 0; i < ragDollColliders.Length; i++)
                Physics.IgnoreCollision(ragDollColliders[i], grabber, ignore);
            if (proxy) proxy.IgnoreCollisionWithThrower(grabber, ignore);
        }
        else Physics.IgnoreCollision(nonRagdolledCollider, grabber, ignore);
    }










    public new void ReplaceWithNew()
    {
        RagdollHandler newRagdoll = gameObject.AddComponent<RagdollHandler>();
         
        newRagdoll.minRagdollTime = minRagdollTime;
        newRagdoll.maxRagdollTime = maxRagdollTime;
        newRagdoll.minRagdollVelocity = minRagdollVelovity;
        newRagdoll.ragDollColliders = ragDollColliders;
        newRagdoll.ragDollRigidBodies = ragDollRigidBodies;
        
        DestroyImmediate(proxy);
    }


}
