using UnityEditor;
using UnityEngine;

namespace SLS.StateMachineV3
{
    /// <summary>
    /// Behavior Scripts attached to a state. Inherit from this to create functionality.
    /// </summary>
    [RequireComponent(typeof(State_OLD))]
    public abstract class StateBehavior : MonoBehaviour
    {

        /// <summary>
        /// The State Machine owning this behavior. Likely the most important field you'll be referencing a lot.<br />
        /// Override with the "new" keyword with an expression like "=> M as MyStateMachine" to get a custom StateMachine
        /// </summary>
        public StateMachine_OLD Machine { get; private set; }
        /// <summary>
        /// An indirection to access the State Machine's gameObject property.
        /// </summary>
        public new GameObject gameObject => Machine.gameObject;
        /// <summary>
        /// An indirection to access the State Machine's transform property.
        /// </summary>
        public new Transform transform => Machine.transform;
        /// <summary>
        /// The current State. Usefull for referencing this SubObject.
        /// </summary>
        public State_OLD state { get; private set; }


        public void Setup(State_OLD @state, bool makeDirty = false)
        {
            Machine = @state.machine;
            this.state = @state;

            this.OnSetup();

#if UNITY_EDITOR
            if (makeDirty) EditorUtility.SetDirty(this);
#endif
        }
        protected virtual void OnSetup() { }


        public virtual void OnAwake() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnEnter(State_OLD prev, bool isFinal) { }
        public virtual void OnExit(State_OLD next) { }

        public C GetComponentFromMachine<C>() where C : Component => Machine.GetComponent<C>();
        public bool TryGetComponentFromMachine<C>(out C result) where C : Component => Machine.TryGetComponent(out result);

        public void TransitionTo(State_OLD nextState) => Machine.TransitionState(nextState);

        public virtual void Activate() => state.TransitionTo();


        public static implicit operator bool(StateBehavior B) => B != null && B.state.active;
    }
}