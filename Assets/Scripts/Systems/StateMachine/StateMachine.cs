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

        [SerializeField] private string rootPath = "Root";

        #endregion

        #region Data

        public State ROOTState { get; private set; }
        public State currentState { get; private set; }
        public State currentTopState => ROOTState.activeChild;
        public State[] topLevelStates => ROOTState.children;
        public StateMachineVariables Variables { get; private set; }

        #endregion







        protected virtual void Awake() => this.Initialize();

        public virtual void Initialize()
        {
            if (ROOTState == null || Variables == null)
            {
                Transform rootObject = transform.Find(rootPath);
                ROOTState = rootObject.GetComponent<State>();
                Variables = ROOTState.GetComponent<StateMachineVariables>();
                Variables.Initialize();
            }
            ROOTState.Initialize(this, -1);
            currentState = ROOTState.activeChild;
        }

        private void Reset()
        {
            Transform tryRoot = transform.Find("Root");
            GameObject root = tryRoot ? tryRoot.gameObject : new GameObject("Root");
            root.transform.parent = transform;
            ROOTState = root.AddComponent<State>();
            Variables = root.AddComponent<StateMachineVariables>();
        }

        protected virtual void Update() => ROOTState.Update_S();

        protected virtual void FixedUpdate() => ROOTState.FixedUpdate_S();

        public virtual void TransitionState(State nextState) => TransitionState(nextState, currentState);
        public virtual void TransitionState(State nextState, State prevState)
        {
            if (nextState == prevState || nextState == currentState || nextState == null ||
                prevState.active == false || prevState == null || prevState == ROOTState) return; 

            int i = prevState.lineage.Length - 1;
            for (; i >= 0;)
            {
                prevState.lineage[i].Exit();
                if (i==0 || nextState.lineage.Contains(prevState.lineage[i-1])) break;  
                i--;
            }
            for (; i < nextState.lineage.Length-1; i++)
                nextState.lineage[i].Enter(false);
            nextState.Enter();
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

}