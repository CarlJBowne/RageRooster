using UnityEngine;
using UnityEngine.Events;
using EditorAttributes;
using System.Linq;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace SLS.StateMachineV3
{
    /// <summary>
    /// The class for an individual State in the State Machine. I wouldn't recommend inheriting from this.
    /// </summary>
    public class State : MonoBehaviour
    {
        #region Config

        [HideField(nameof(__isMachine))] public bool locked = false;
        /// <summary>
        /// Acts as a separate state from children rather than automating to the first in the list. Only applicable if this State has child states. 
        /// </summary>
        [SerializeField, ShowField(nameof(__showSepFromChildren))] private bool separateFromChildren;
        [SerializeField, HideField(nameof(__isMachine))] public UnityEvent<State> onActivatedEvent;
        


        #region Buttons

        [Button]
        protected virtual void AddChild()
        {
            var NSGO = new GameObject("NewState");
            NSGO.transform.parent = base.transform;
            NSGO.AddComponent<State>();
        }
        [Button(nameof(__enableSiblingCreation), ConditionResult.EnableDisable)]
        protected virtual void AddSibling()
        {
            var NSGO = new GameObject("NewState");
            NSGO.transform.parent = base.transform.parent;
            NSGO.AddComponent<State>();
        }

        #endregion Buttons

        #endregion

        #region Data

        //Inherited Components from Machine.
        public StateMachine machine { get; protected set; }
        //Data
        public bool active { get; protected set; }
        public StateBehavior[] behaviors { get; protected set; }

        //Layer Data
        public int layer { get; protected set; }

        //Relationships.
        public State parent { get; protected set; }
        public State[] children { get; protected set; }
        public int childCount { get; protected set; }
        public State activeChild { get; protected set; }
        public State[] lineage { get; protected set; }




        //Getters

        public State this[int i] => children[i];
        public StateBehavior this[System.Type T] => GetComponent(T) as StateBehavior;
        public T Behavior<T>() where T : StateBehavior => behaviors.First(x => x is T) as T;
        public static implicit operator bool(State s) => s.active;

        #endregion

        #region EditorData
        protected virtual bool __showSepFromChildren => base.transform.childCount > 0 && __enableSiblingCreation;
        protected virtual bool __enableSiblingCreation => true;
        protected virtual bool __isMachine => false;


        #endregion 

        private void InitializeP(StateMachine machine, State parent, int layer)
        {
            this.machine = machine;
            this.layer = layer;

            this.parent = parent;
            gameObject.SetActive(false);

            {
                lineage = new State[layer + 1];
                State iState = this;
                for (int i = layer; i >= 0; i--)
                {
                    lineage[i] = iState;
                    iState = iState.parent;
                }
            }//Lineage Setup

            SetupChildren(transform);

            behaviors = GetComponents<StateBehavior>();
            behaviors.DoInit(this);
        }

        protected void SetupChildren(Transform parent)
        {
            childCount = parent.childCount;
            children = new State[childCount];
            for (int i = 0; i < childCount; i++)
            {
                children[i] = parent.GetChild(i).GetComponent<State>();
                children[i].InitializeP(machine, this, layer + 1);
            }//Children Setup
        }


        public void DoAwake()
        {
            behaviors.DoAwake();
            children.DoAwake();
        }

        public void DoUpdate()
        {
            behaviors.DoUpdate();
            if (childCount>0 && activeChild) activeChild.DoUpdate();
        }
        public void DoFixedUpdate()
        {
            behaviors.DoFixedUpdate();
            if (childCount > 0 && activeChild) activeChild.DoFixedUpdate();
        }
        public void EnterState(State prev, bool specifically = true)
        {
            if(parent!=null) parent.activeChild = this;
            active = true;

            behaviors.DoEnter(prev);

            if (specifically && childCount>0 && !separateFromChildren)
            {
                activeChild = children[0];
                activeChild.EnterState(prev, specifically);
            }
                
            base.gameObject.SetActive(true);
        }
        public void ExitState(State next)
        {
            parent.activeChild = null;
            active = false;
            behaviors.DoExit(next);
            base.gameObject.SetActive(false);
        }

        public void TransitionTo() => machine.TransitionState(this);
    }

    /// <summary>
    /// Behavior Scripts attached to a state. Inherit from this to create functionality.
    /// </summary>
    [RequireComponent(typeof(State))]
    public abstract class StateBehavior : MonoBehaviour
    {

        /// <summary>
        /// The State Machine owning this behavior. Likely the most important field you'll be referencing a lot.<br />
        /// Override with the "new" keyword with an expression like "=> M as MyStateMachine" to get a custom StateMachine
        /// </summary>
        public StateMachine M { get; private set; }
        public new GameObject gameObject => M.gameObject;
        public new Transform transform => M.transform;
        public State state { get; private set; }


        public void InitializeP(State @state)
        {
            M = @state.machine;
            this.state = @state;

            this.Initialize();
        }
        protected virtual void Initialize() { }


        public virtual void OnAwake() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnEnter(State prev) { }
        public virtual void OnExit(State next) { }

        public C GetComponentFromMachine<C>() where C : Component => M.GetComponent<C>();
        public bool TryGetComponentFromMachine<C>(out C result) where C : Component => M.TryGetComponent(out result);

        public void TransitionTo(State nextState) => M.TransitionState(nextState);

        public virtual void Activate() => state.TransitionTo();


        public static implicit operator bool(StateBehavior B) => B != null && B.state.active;
    }

    public static class _StateMachineExtMethods
    {
        public static void DoAwake(this State[] states)
        { for (int i = 0; i < states.Length; i++) states[i].DoAwake(); }

        public static void DoInit(this StateBehavior[] beahviors, State This)
        { for (int i = 0; i < beahviors.Length; i++) beahviors[i].InitializeP(This); }
        public static void DoAwake(this StateBehavior[] beahviors)
        { for (int i = 0; i < beahviors.Length; i++) beahviors[i].OnAwake(); }
        public static void DoUpdate(this StateBehavior[] beahviors)
        { for (int i = 0; i < beahviors.Length; i++) beahviors[i].OnUpdate(); }
        public static void DoFixedUpdate(this StateBehavior[] beahviors)
        { for (int i = 0; i < beahviors.Length; i++) beahviors[i].OnFixedUpdate(); }
        public static void DoEnter(this StateBehavior[] states, State prev)
        { for (int i = 0; i < states.Length; i++) states[i].OnEnter(prev); }
        public static void DoExit(this StateBehavior[] states, State next)
        { for (int i = 0; i < states.Length; i++) states[i].OnExit(next); }

        public static bool IsTopLayer(this State state) => state.layer == 0;
        public static bool ActiveMain(this State state) => state.machine.currentState == state;
        public static bool IsMachine(this State state) => state is StateMachine;
    }
}