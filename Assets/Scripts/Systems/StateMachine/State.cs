using UnityEngine;
using UnityEngine.Events;
using EditorAttributes;

namespace SLS.StateMachineV2
{
    /// <summary>
    /// The class for an individual State in the State Machine. You can't inherit from this.
    /// </summary>
    public sealed class State : MonoBehaviour
    {
        #region Config

        /// <summary>
        /// Acts as a separate state from children rather than automating to the first in the list. Only applicable if this State has child states. 
        /// </summary>
        [SerializeField, ShowField(nameof(__showSepFromChildren))] private bool separateFromChildren;
        [SerializeField] public UnityEvent<State> onActivatedEvent;


        #region Buttons

        [Button]
        private void AddChild()
        {
            var NSGO = new GameObject("NewState");
            NSGO.transform.parent = base.transform;
            NSGO.AddComponent<State>();
        }
        [Button(nameof(__enableSiblingCreation), ConditionResult.EnableDisable)]
        private void AddSibling()
        {
            var NSGO = new GameObject("NewState");
            NSGO.transform.parent = base.transform.parent;
            NSGO.AddComponent<State>();
        }

        #endregion Buttons

        #endregion

        #region Data

        //Inherited Components from Machine.
        public StateMachine machine { get; private set; }
        //Data
        public bool active { get; private set; }
        public StateBehavior[] behaviors { get; private set; }
        public bool isMultiState { get; private set; }
        public bool isRoot => layer == -1;
        public bool isTopLayer => layer == 0;

        //Relationships.
        public State parent { get; private set; }
        public int layer { get; private set; }
        public State[] children { get; private set; }
        public State activeChild { get; private set; }
        public State[] lineage { get; private set; }

        public State this[int i] => children[i];

        public bool activeMain => machine.currentState == this;

        #endregion

        #region EditorData
        public bool __showSepFromChildren => base.transform.childCount > 0 && __enableSiblingCreation;
        public bool __enableSiblingCreation => layer != -1;

        #endregion 

        private void Reset()
        {
            if (transform.parent.TryGetComponent<StateMachine>(out _)) layer = -1;
        }

        public void _Initialize(StateMachine machine, int layer)
        {
            this.machine = machine;
            this.layer = layer;

            if(layer != -1) parent = base.transform.parent.GetComponent<State>();
            else separateFromChildren = false;

            LineageSetup();

            int childrenCount = base.transform.childCount;
            if (layer == -1 && childrenCount < 0) throw new System.Exception("Why on earth do you have a State Machine with zero states?");
            if (childrenCount > 0)
            {
                isMultiState = true;
                children = new State[childrenCount];
                for (int i = 0; i < childrenCount; i++)
                {
                    children[i] = base.transform.GetChild(i).GetComponent<State>();
                    children[i]._Initialize(machine, layer + 1);
                }
            }

            behaviors = GetComponents<StateBehavior>();
            for (int i = 0; i < behaviors.Length; i++) behaviors[i]._Initialize(machine);

            if(layer == -1) _Enter(true);
        }


        private void LineageSetup()
        {
            var result = new State[layer + 1];
            State iState = this;
            for (int i = layer; i >= 0; i--)
            {
                result[i] = iState;
                iState = iState.parent;
            }
            lineage = result;
        }


        public void _Update()
        {
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnUpdate();
            if (isMultiState) activeChild?._Update();
        }
        public void _FixedUpdate()
        {
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnFixedUpdate();
            if (isMultiState) activeChild?._FixedUpdate();
        }
        public void _Enter(bool specifically = true)
        {
            if(parent!=null) parent.activeChild = this;
            active = true;

            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnEnter();

            if (specifically && isMultiState && !separateFromChildren)
            {
                activeChild = children[0];
                activeChild._Enter(specifically);
            }
                
            base.gameObject.SetActive(true);
        }
        public void _Exit()
        {
            parent.activeChild = null;
            active = false;
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnExit();
            base.gameObject.SetActive(false);
        }


        public C GetComponentFromMachine<C>() where C : Component => machine.GetComponent<C>();
        public bool TryGetComponentFromMachine<C>(out C result) where C : Component => machine.TryGetComponent(out result);

        public void TransitionTo(State nextState) => machine.TransitionState(nextState, this);
        public void TransitionTo() => machine.TransitionState(this);

        public static implicit operator bool(State s) => s.active;

    }

}