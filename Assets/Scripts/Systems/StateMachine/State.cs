using UnityEngine;
using EditorAttributes;
using System.Linq;
using AYellowpaper.SerializedCollections;
using UltEvents;

namespace SLS.StateMachineV3
{
    //NOTE TO SELF: State Machine inheriting from State and thus gaining all of its State-specific data pieces is a pain in the ass. Look into fixing later with a shared Ancestor.
    //NOTE TO SELF 2: Multi-Layered State Machines are also a Pain in the ass. Consider Version 3 where there's no layering but each State can be in a group.
    //NOTE TO SELF 3: Maybe have "Previous State" as a saved reference in Machin in V3?

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
        [SerializeField, HideField(nameof(__isMachine))] public UltEvent<State> onActivatedEvent;

        #region Signals
        [SerializeField, HideField(nameof(__isMachine))] public SerializedDictionary<string, UltEvent> signals;
        [SerializeField, HideField(nameof(__isMachine))] public bool lockReady;
        #endregion 

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
        protected virtual bool __isntMachine => true;


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
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].InitializeP(this);
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
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnAwake();
            for (int i = 0; i < children.Length; i++) children[i].DoAwake();
        }

        public void DoUpdate()
        {
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnUpdate();
            if (childCount>0 && activeChild != null) activeChild.DoUpdate();
        }
        public void DoFixedUpdate()
        {
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnFixedUpdate();
            if (childCount > 0 && activeChild != null) activeChild.DoFixedUpdate(); 
        }
        public State EnterState(State prev, bool specifically = true)
        {
            if(parent!=null) parent.activeChild = this;
            active = true;
            base.gameObject.SetActive(true);

            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnEnter(prev, specifically && (childCount == 0 || separateFromChildren));

            if (specifically && childCount > 0 && !separateFromChildren)
            {
                activeChild = children[0];
                return activeChild.EnterState(prev, specifically);
            }
            else machine.signalReady = !lockReady; 
            return this;
        }
        public void ExitState(State next)
        {
            parent.activeChild = null;
            active = false;
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnExit(next);
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
        /// <summary>
        /// An indirection to access the State Machine's gameObject property.
        /// </summary>
        public new GameObject gameObject => M.gameObject;
        /// <summary>
        /// An indirection to access the State Machine's transform property.
        /// </summary>
        public new Transform transform => M.transform;
        /// <summary>
        /// The current State. Usefull for referencing this SubObject.
        /// </summary>
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
        public virtual void OnEnter(State prev, bool isFinal) { }
        public virtual void OnExit(State next) { }

        public C GetComponentFromMachine<C>() where C : Component => M.GetComponent<C>();
        public bool TryGetComponentFromMachine<C>(out C result) where C : Component => M.TryGetComponent(out result);

        public void TransitionTo(State nextState) => M.TransitionState(nextState);

        public virtual void Activate() => state.TransitionTo();


        public static implicit operator bool(StateBehavior B) => B != null && B.state.active;
    }

    public static class _StateMachineExtMethods
    {
        public static bool IsTopLayer(this State state) => state.layer == 0;
        public static bool ActiveMain(this State state) => state.machine.currentState == state;
        public static bool IsMachine(this State state) => state is StateMachine;
    }
}