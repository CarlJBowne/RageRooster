using UnityEngine;
using UnityEngine.Events;

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
        [SerializeField] private bool separateFromChildren;
        [SerializeField] public UnityEvent<State> onActivatedEvent; 

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
        #endregion






        public void Initialize(StateMachine machine, int layer)
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
                    children[i].Initialize(machine, layer + 1);
                }
            }

            behaviors = GetComponents<StateBehavior>();
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].Initialize(machine);
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].Awake_S();

            Enter();
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


        public void Update_S()
        {
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].Update_S();
            if (isMultiState) activeChild?.Update_S();
        }
        public void FixedUpdate_S()
        {
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].FixedUpdate_S();
            if (isMultiState) activeChild?.FixedUpdate_S();
        }
        public void Enter()
        {
            if(parent!=null) parent.activeChild = this;
            active = true;

            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnEnter();

            if (isMultiState && !separateFromChildren)
            {
                activeChild = children[0];
                activeChild.Enter();
            }
                
            base.gameObject.SetActive(true);
        }
        public void Exit()
        {
            parent.activeChild = null;
            active = false;
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnExit();
            base.gameObject.SetActive(false);
        }


        public C GetComponentFromMachine<C>() where C : Component => machine.GetComponent<C>();
        public bool TryGetComponentFromMachine<C>(out C result) where C : Component => machine.TryGetComponent(out result);

        public void TransitionTo(State nextState) => machine.TransitionState(nextState, this);

        public static implicit operator bool(State s) => s.active;

    }

}