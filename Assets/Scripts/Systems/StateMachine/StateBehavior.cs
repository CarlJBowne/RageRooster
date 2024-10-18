using Unity.VisualScripting;
using UnityEngine;

namespace SLS.StateMachineV2
{
    /// <summary>
    /// Behavior Scripts attached to a state. Inherit from this to create functionality.
    /// </summary>
    [RequireComponent(typeof(State))]
    public abstract class StateBehavior : MonoBehaviour
    {
        #region Config


        #endregion

        #region Data

        /// <summary>
        /// The State Machine owning this behavior. Likely the most important field you'll be referencing a lot.<br />
        /// Override with the "new" keyword with an expression like "=> M as MyStateMachine" to get a custom StateMachine
        /// </summary>
        public StateMachine M { get; private set; }
        public new GameObject gameObject { get; private set; }
        public new Transform transform { get; private set; }
        public State state { get; private set; }

        #endregion






        public virtual void Initialize(StateMachine machine)
        {
            this.M = machine;
            gameObject = machine.gameObject;
            transform = gameObject.transform;
            state = GetComponent<State>();

            this.Awake_S();
        }

        public virtual void Awake_S() { }
        public virtual void Update_S() { }
        public virtual void FixedUpdate_S() { }
        public virtual void OnEnter() { }
        public virtual void OnExit() { }

        public C GetComponentFromMachine<C>() where C : Component => M.GetComponent<C>();
        public bool TryGetComponentFromMachine<C>(out C result) where C : Component => M.TryGetComponent(out result);

        /// <summary>
        /// Get a StateBehavior attached to the Root State. (Aka. a Global State Behavior)
        /// </summary>
        /// <typeparam name="T">The Type wanted.</typeparam>
        public T GetGlobalBehavior<T>() where T : StateBehavior => M.ROOTState.GetComponent<T>();
        /// <summary>
        /// Try to get a StateBehavior attached to the Root State. (Aka. a Global State Behavior)
        /// </summary>
        /// <typeparam name="T">The Type wanted.</typeparam>
        /// <param name="result">The resulting behavior</param>
        /// <returns>True if a behavior of that type exists exists.</returns>
        public bool TryGetGlobalBehavior<T>(out T result) where T : StateBehavior => M.ROOTState.TryGetComponent<T>(out result);


        public void TransitionTo(State nextState) => M.TransitionState(nextState);

    }
}