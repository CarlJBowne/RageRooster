using System;
using UnityEngine;
using SLS.StateMachineV2;
using EditorAttributes;
using UnityEngine.Events;


public class TrackerEB : StateBehavior
{
    public Transform target;

    

    public float autoCheckRate;

    public bool updateDistance;
    public bool updateDot;
    public bool updateLineOfSight;

    [HideInInspector] public LayerMask LOSMask;

    [HelpBox("When check is run, the highest phase whose requirements are satisfied is transitioned to. These phases are equal to this state's child states, in order. The 0th state is treated as default so its parameters don't really matter.")]
    public Phase[] phases;
    private int currentPhase;  
    [System.Serializable]
    public struct Phase
    {
        public float distance;
        [Tooltip("from 0 to 180 degrees")]
        public float dot;
        public bool lineOfSight;
        [Tooltip("1 = full second turn, 50 = 1 FixedUpdate turn")]
        public float autoRotateDelta;

        public bool Within(TrackerEB tracker)
        {
            bool result = true;

            if (tracker.updateDistance && tracker.distance > distance) result = false;
            if (tracker.updateDot && tracker.dot > dot) result = false;
            if (tracker.updateLineOfSight && tracker.lineOfSight != lineOfSight) result = false;

            return result;
        }

    }

    private Timer autoCheckTimer;
    private float distance;
    private float dot;
    private bool lineOfSight;

    public override void OnAwake()
    {
        if(target == null)
        {
            PlayerStateMachine attempt = FindObjectOfType<PlayerStateMachine>();
            if (attempt != null) target = attempt.transform; 
            else PlayerStateMachine.whenInitializedEvent += player => 
            { 
                target = player.transform; 
            };
        }
        if (autoCheckRate > 0) autoCheckTimer = new(autoCheckRate, CheckData);
    }

    public override void OnEnter()
    {
        if (target == null)
        {
            return;
        }
        Distance(true);
        Dot(true);
        LineOfSight(true);
        CheckData();
    }

    public override void OnUpdate()
    {
        if (phases[currentPhase].autoRotateDelta > 0) 
            transform.eulerAngles = Vector3.RotateTowards(transform.forward, Direction.XZ(),
                phases[currentPhase].autoRotateDelta * Mathf.PI * Time.fixedDeltaTime, 0)
                .DirToRot();

        if (autoCheckRate > 0) autoCheckTimer += Time.deltaTime;
        else if(autoCheckRate == 0) CheckData();
    }

    private void CheckData()
    {
        if (updateDistance) Distance(true);
        if (updateDot) Dot(true);
        if (updateLineOfSight) LineOfSight(true);

        int i = phases.Length - 1;
        for (; i > 0; i--) 
        {
            if (phases[i].Within(this)) break;
        }

        if (i != currentPhase) PhaseTransition(i);

    }

    public void PhaseTransition(int i)
    {
        if (i == currentPhase) return;
        currentPhase = i;
        state[currentPhase].TransitionTo();
    }
    public void PhaseTransition(State i) => PhaseTransition(i.transform.GetSiblingIndex());

    public float Distance(bool check)
    {
        if (check) distance = Vector3.Distance(transform.position, target.position);
        return distance;
    }
    public float Dot(bool check)
    {
        if (check) dot = (Vector3.Dot(transform.forward, target.position - transform.position) - 1) * -1 * 90;
        return dot;
    }
    public bool LineOfSight(bool check)
    {
        if (check)
        {
            Physics.Linecast(transform.position, target.position, out RaycastHit hit, LOSMask);
            lineOfSight = hit.transform == target;
        }
        return lineOfSight;
    }
    public Vector3 Direction => target.position - transform.position;

}