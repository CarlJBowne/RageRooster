using System.Linq;
using UnityEngine;

namespace SLS.StateMachineV2
{
    /// <summary>
    /// The Overarching controller of a State Machine object. <br />
    /// Override this class to create more specific StateMachines with more easily accessible components. <br />
    /// Although most of the time it's probably not necessary.
    /// </summary>
    public class StateMachine : MonoBehaviour
    {

        #region Config

        //Note, make nonreliant on YellowPaper later.
        public AYellowpaper.SerializedCollections.SerializedDictionary<string, State> states;

        #endregion

        #region Data

        public State ROOTState
        {
            get
            {
                if (_ROOTState == null) _ROOTState = transform.Find("Root").GetComponent<State>();
                return _ROOTState;
            }
            private set => _ROOTState = value;
        }
        private State _ROOTState { get; set; } 
        public State currentState { get; private set; }
        public State currentTopState => ROOTState.activeChild;
        public State[] topLevelStates => ROOTState.children;
        public StateMachineVariables Variables { get; private set; }

#nullable enable
        public System.Action<PhysicsCallback, Collision?, Collider?> physicsCallbacks;
#nullable disable
        public System.Action waitforMachineInit;

        #endregion







        protected virtual void Awake() => this._Initialize();

        private void _Initialize()
        {
            if (ROOTState == null || Variables == null)
            {
                Transform rootObject = transform.Find("Root");
                if(rootObject == null)
                {
                    int i = 0;
                    for (; i < rootObject.childCount; i++)
                    {
                        Transform child = rootObject.GetChild(i);
                        if (child.TryGetComponent<State>(out _))
                        {
                            child.name = "Root";
                            rootObject = child;
                            break;
                        }
                    }
                    if (i == rootObject.childCount)
                    {
                        enabled = false;
                        throw new System.Exception("Root State Missing");
                    }
                }
                ROOTState = rootObject.GetComponent<State>();
                Variables = ROOTState.GetComponent<StateMachineVariables>();
                Variables.Initialize();
            }
            this.Initialize();
            ROOTState._Initialize(this, -1);
            currentState = ROOTState.activeChild;
            waitforMachineInit?.Invoke();
        }

        protected virtual void Initialize() { }

        private void Reset()
        {
            Transform tryRoot = transform.Find("Root");
            GameObject root = tryRoot ? tryRoot.gameObject : new GameObject("Root");
            root.transform.parent = transform;
            ROOTState = root.AddComponent<State>();
            Variables = root.AddComponent<StateMachineVariables>();
        }

        protected virtual void Update() => ROOTState._Update();

        protected virtual void FixedUpdate() => ROOTState._FixedUpdate();

        #region Physics Callbacks
        private void OnCollisionEnter(Collision collision) => physicsCallbacks?.Invoke(PhysicsCallback.OnCollisionEnter, collision, null);
        private void OnCollisionExit(Collision collision) => physicsCallbacks?.Invoke(PhysicsCallback.OnCollisionExit, collision, null);
        private void OnTriggerEnter(Collider other) => physicsCallbacks?.Invoke(PhysicsCallback.OnTriggerEnter, null, other);
        private void OnTriggerExit(Collider other) => physicsCallbacks?.Invoke(PhysicsCallback.OnTriggerExit, null, other);
        #endregion

        public virtual void TransitionState(State nextState) => TransitionState(nextState, currentState);
        public virtual void TransitionState(State nextState, State prevState)
        {
            {
                if (nextState == prevState ||
                    nextState == currentState ||
                    nextState == null ||
                    prevState.active == false ||
                    prevState == null ||
                    prevState == ROOTState ||
                    nextState.locked
                ) return;
            } // Pre Checks

            int i = prevState.lineage.Length - 1;
            for (; i >= 0;)
            {
                prevState.lineage[i]._Exit();
                if (i==0 || nextState.lineage.Contains(prevState.lineage[i-1])) break;  
                i--;
            }
            for (; i < nextState.lineage.Length-1; i++)
                nextState.lineage[i]._Enter(false);
            nextState._Enter();
            currentState = nextState;
            nextState.onActivatedEvent?.Invoke(prevState);
        }

        /// <summary>
        /// Get a StateBehavior attached to the Root State. (Aka. a Global State Behavior)
        /// </summary>
        /// <typeparam name="T">The Type wanted.</typeparam>
        public T GetGlobalBehavior<T>() where T : StateBehavior => ROOTState.GetComponent<T>();
        /// <summary>
        /// Try to get a StateBehavior attached to the Root State. (Aka. a Global State Behavior)
        /// </summary>
        /// <typeparam name="T">The Type wanted.</typeparam>
        /// <param name="result">The resulting behavior</param>
        /// <returns>True if a behavior of that type exists exists.</returns>
        public bool TryGetGlobalBehavior<T>(out T result) where T : StateBehavior => ROOTState.TryGetComponent<T>(out result);

    }

    public enum PhysicsCallback
    {
        OnCollisionEnter, OnCollisionExit, 
        OnTriggerEnter, OnTriggerExit
            //, OnCollisionStay, OnTriggerStay
    }

}