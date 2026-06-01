using System.Collections.Generic;
using SLS.StateMachineH;
using SLS.StateMachineH.Timelines;
using UnityEngine;

#if ULT_EVENTS
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

public class StateTransitions : StateTimeline
{
    [System.Serializable]
    public class Transition
    {
        [field: SerializeField] public State TargetState { get; private set; }
        [Tooltip("Wait until the end of the length coroutine to transition officially in the state graph.")]
        [field: SerializeField] public bool AtEnd { get; private set; }
        [field: SerializeField] public EVENT BeginEvent { get; private set; }
        [field: SerializeField] public float Length { get; private set; }
        [field: SerializeField] public EVENT EndEvent { get; private set; }
        [field: SerializeField] public AnimatorAction Animation { get; private set; }
    }

    public List<Transition> transitions = new();
    [SerializeField] private Animator animator;

    Transition activeTransition = null;
    float timer = 0f;

    public Transition this[int i] => transitions[i];
    public void FireTransition(int i)
    {
        activeTransition = this[i];
        if (activeTransition == null) return;
        Begin();
    }
    protected override void OnBegin()
    {
        if (!activeTransition.AtEnd && activeTransition.TargetState != null) DoEnter();

        activeTransition.BeginEvent.Invoke();

        if (activeTransition.Animation != null && activeTransition.Animation.type is not AnimatorAction.Type.Null)
        {
            activeTransition.Animation.Do(animator);
            //Disable StateAnimator on Target once disabling is implemented
        }

        if (activeTransition.Length <= 0f) End();
    }
    protected override void OnTick(float delta)
    {
        timer += delta;
        if (timer >= activeTransition.Length) End();
    }
    protected override void OnEnd()
    {
        if (activeTransition.AtEnd && activeTransition.TargetState != null) DoEnter();
        activeTransition.EndEvent.Invoke();
        activeTransition = null;
        timer = -1f;
    }

    void DoEnter()
    {
        if (activeTransition == null) return;
        if (activeTransition.Animation && activeTransition.TargetState.TryGetComponent(out StateAnimator anim)) anim.BlockForThisCycle();
        activeTransition.TargetState.Enter();
    }
}
