using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace SLS.StateMachineH {

    /// <summary>  
    /// A state within a Hierarchical <see cref="StateMachine"/>.  
    /// <br /> This class provides functionality for managing state behaviors, relations within a <see cref="StateMachine"/> Tree, and action callbacks.  
    /// <br /> The <see cref="StateMachine"/> inherits from this class, allowing it to function as a root state with additional capabilities.  
    /// <br /> Use <see cref="StateBehavior"/> for adding functionality.  
    /// </summary>  
    public class State : MonoBehaviour
    {
        /// <summary>  
        /// The <see cref="StateBehavior"/>s associated with this <see cref="State"/>.  
        /// </summary>  
        [field: SerializeField] public StateBehavior[] Behaviors { get; internal set; }

        /// <summary>  
        /// The <see cref="StateMachine"/> that owns this <see cref="State"/>.  
        /// </summary>  
        [field: SerializeField] public virtual StateMachine Machine { get; internal set; }

        /// <summary>  
        /// The parent <see cref="State"/> of this <see cref="State"/>. (Will be the <see cref="StateMachine"/> if highest layer.)  
        /// </summary>  
        [field: SerializeField] public State Parent { get; internal set; }

        /// <summary>  
        /// The number of layers down this <see cref="State"/> is within the hierarchy.  
        /// </summary>  
        [field: SerializeField] public virtual int Layer { get; internal set; }

        /// <summary>  
        /// The child <see cref="State"/>s of this <see cref="State"/>, if any exist.  
        /// </summary>  
        [field: SerializeField, SerializeReference] public List<State> Children { get; internal set; } = new();

        /// <summary>  
        /// The number of child <see cref="State"/>s.  
        /// </summary>  
        [field: SerializeField] public int ChildCount { get; internal set; }

        /// <summary>  
        /// Indicates whether this <see cref="State"/> is currently active.  
        /// </summary>  
        public virtual bool Active { get; internal set; }

        /// <summary>  
        /// The currently active child <see cref="State"/>, if any exists.  
        /// </summary>  
        public State CurrentChild
        {
            get => _currentChild;
            internal set
            {
                if (value != null && !Children.Contains(value)) throw new System.Exception("GENUINELY HOW?????");
                _currentChild = value;
            }
        }

        private State _currentChild;

        /// <summary>  
        /// The type of this <see cref="State"/>, either Group or End.  
        /// </summary>  
        public virtual StateType Type => ChildCount > 0
            ? StateType.Group
            : StateType.End;

        /// <summary>  
        /// Whether this <see cref="State"/> has child states.  
        /// </summary>  
        public virtual bool HasChildren => ChildCount > 0;

        /// <summary>  
        /// The name of this <see cref="State"/> or <see cref="StateMachine"/>, derived from the GameObject name.  
        /// </summary>  
        public new string name => gameObject.name;

        /// <summary>  
        /// Sets up the <see cref="State"/> with the specified parameters.  
        /// </summary>  
        /// <param name="machine">The <see cref="StateMachine"/> owning this <see cref="State"/>.</param>  
        /// <param name="parent">The parent <see cref="State"/>.</param>  
        /// <param name="layer">The layer index of this <see cref="State"/>.</param>  
        /// <param name="makeDirty">Whether to mark the <see cref="State"/> as dirty in the editor.</param>  
        public virtual void Setup(StateMachine machine, State parent, int layer, bool makeDirty = false)
        {
            this.Machine = machine;
            this.Parent = parent;
            this.Layer = layer;
            Active = false;
            gameObject.SetActive(false);
            Behaviors = GetComponents<StateBehavior>();
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i].Setup(this, makeDirty);

            {
                ChildCount = transform.childCount;
                Children = new();
                for (int i = 0; i < ChildCount; i++)
                {
                    Children.Add(transform.GetChild(i).GetComponent<State>());
                    Children[i].Setup(machine, this, layer + 1);
                }
            }

#if UNITY_EDITOR
            if (makeDirty) EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>  
        /// Invokes the awake behavior for this <see cref="State"/> and its children.  
        /// </summary>  
        internal void DoAwake()
        {
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i].DoAwake();

            for (int i = 0; i < Children.Count; i++)
                Children[i].DoAwake();
        }

        /// <summary>  
        /// Invokes the update behavior for this <see cref="State"/> and its active child.  
        /// </summary>  
        internal void DoUpdate()
        {
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i].DoUpdate();

            CurrentChild?.DoUpdate();
        }

        /// <summary>  
        /// Invokes the fixed update behavior for this <see cref="State"/> and its active child.  
        /// </summary>  
        internal void DoFixedUpdate()
        {
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i].DoFixedUpdate();

            if (CurrentChild) CurrentChild.DoFixedUpdate();
        }

        /// <summary>  
        /// Handles entering this <see cref="State"/>, activating it and invoking behaviors.  
        /// </summary>  
        /// <param name="prev">The previous <see cref="State"/>.</param>  
        internal void DoEnter(State prev)
        {
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i].DoEnter(null, !HasChildren);
            Active = true;
            gameObject.SetActive(true);
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i].DoEnter(prev, !HasChildren);
        }

        /// <summary>  
        /// Handles exiting this <see cref="State"/>, deactivating it and invoking behaviors.  
        /// </summary>  
        /// <param name="next">The next <see cref="State"/>.</param>  
        internal void DoExit(State next)
        {
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i].DoExit(null);
            Active = false;
            gameObject.SetActive(false);
            CurrentChild = null;
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i].DoExit(next);
        }

        /// <summary>  
        /// Tells the <see cref="StateMachine"/> to begin Transitioning from its current <see cref="State"/> to this <see cref="State"/>.  
        /// </summary>  
        [ContextMenu("Enter")]
        public void Enter() => Machine.TransitionState(this);

        /// <summary>  
        /// Adds a child <see cref="State"/> below this <see cref="State"/> / <see cref="StateMachine"/>.  
        /// </summary>  
        /// <returns>The newly created child <see cref="State"/>.</returns>  
        public State AddChildNode()
        {
            GameObject newObject = new("New State");
            newObject.transform.SetParent(this is StateMachine SM ? SM.StateHolder : transform, false);
            State newNode = newObject.AddComponent<State>();
            Children.Add(newNode);
            ChildCount++;
            newNode.Setup(Machine, this, Layer + 1, true);

#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(newObject, "Add Child Node");
            Undo.RecordObject(this, "Add Child Node");
            EditorUtility.SetDirty(Machine);
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(newNode);
#endif
            return newNode;
        }


        public State this[int i] => Children[i];

        /// <summary>
        /// Implicit bool operator. Returns true if the State exists and is Active.
        /// </summary>
        public static implicit operator bool(State state) => state != null && state.Active;

    }

    public enum StateType
    {
        /// <summary>  
        /// Represents a terminal <see cref="State"/> with no children.  
        /// </summary>  
        End,

        /// <summary>  
        /// Represents a <see cref="State"/> that groups other states.  
        /// </summary>  
        Group,

        /// <summary>  
        /// Represents the root <see cref="StateMachine"/>.  
        /// </summary>  
        Machine
    }
}