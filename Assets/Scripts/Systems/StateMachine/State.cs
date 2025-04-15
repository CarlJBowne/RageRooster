using UnityEngine;
using EditorAttributes;
using System.Linq;
using AYellowpaper.SerializedCollections;
using UltEvents;
using UnityEditor;

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

        [FoldoutGroup("Lifetime Events", nameof(onAwakeEvent), nameof(onEnterEvent), nameof(onExitEvent), nameof(onActiveChangeEvent), nameof(onUpdateEvent), nameof(onFixedUpdateEvent))]
        public Void lifetimeEventsHolder;

        [SerializeField, HideInInspector] public UltEvent<StateMachine> onAwakeEvent;
        [SerializeField, HideInInspector] public UltEvent<State> onEnterEvent;
        [SerializeField, HideInInspector] public UltEvent<State> onExitEvent;
        [SerializeField, HideInInspector] public UltEvent<bool> onActiveChangeEvent;
        [SerializeField, HideInInspector] public UltEvent<float> onUpdateEvent;
        [SerializeField, HideInInspector] public UltEvent<float> onFixedUpdateEvent;


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


        //Data

        public bool active { get; protected set; }

        public StateMachine machine { get => _machine; protected set => _machine = value; }
        private StateMachine _machine;
        public int layer { get => _layer; protected set => _layer = value; }
        private int _layer;
        public State parent { get => _parent; protected set => _parent = value; }
        private State _parent;
        public State[] children { get => _children; protected set => _children = value; }
        private State[] _children;
        public int childCount { get => _childCount; protected set => _childCount = value; }
        private int _childCount;
        public State activeChild { get => _activeChild; protected set => _activeChild = value; }
        private State _activeChild;
        public State[] lineage { get => _lineage; protected set => _lineage = value; }
        private State[] _lineage;
        public StateBehavior[] behaviors { get => _behaviors; protected set => _behaviors = value; }
        private StateBehavior[] _behaviors;

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

        public virtual void Setup(StateMachine machine, State parent, int layer, bool makeDirty = false)
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

            {
                childCount = transform.childCount;
                children = new State[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    children[i] = transform.GetChild(i).GetComponent<State>();
                    children[i].Setup(machine, this, layer + 1);
                }
            }//Children Setup

            behaviors = GetComponents<StateBehavior>();
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].Setup(this);

#if UNITY_EDITOR
            if(makeDirty) EditorUtility.SetDirty(this);
#endif
        }











        public void DoAwake()
        {
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnAwake();
            for (int i = 0; i < children.Length; i++) children[i].DoAwake();
        }

        public void DoUpdate()
        {
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnUpdate();
            onUpdateEvent?.Invoke(Time.deltaTime);
            if (childCount>0 && activeChild != null) activeChild.DoUpdate();
        }
        public void DoFixedUpdate()
        {
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnFixedUpdate();
            onFixedUpdateEvent?.Invoke(Time.fixedDeltaTime);
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
            onEnterEvent?.DynamicInvoke(prev);
            onActiveChangeEvent?.DynamicInvoke(true);
            return this;
        }
        public void ExitState(State next)
        {
            parent.activeChild = null;
            active = false;
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnExit(next);
            base.gameObject.SetActive(false);
            onExitEvent?.DynamicInvoke(next);
            onActiveChangeEvent?.DynamicInvoke(false);
        }

        public void TransitionTo() => machine.TransitionState(this);


        
    }

    public static class _StateMachineExtMethods
    {
        public static bool IsTopLayer(this State state) => state.layer == 0;
        public static bool ActiveMain(this State state) => state.machine.currentState == state;
        public static bool IsMachine(this State state) => state is StateMachine;
    }
}