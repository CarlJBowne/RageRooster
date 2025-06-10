using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using EditorAttributes;
using AYellowpaper.SerializedCollections;
using UltEvents;
using static UnityEngine.InputSystem.OnScreen.OnScreenStick;
using UnityEditor;

namespace SLS.StateMachineV3
{
    /// <summary>
    /// The Overarching controller of a State Machine object. <br />
    /// Override this class to create more specific StateMachines with more easily accessible components. <br />
    /// Although most of the time it's probably not necessary.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class StateMachine : State
    {

        #region Config

        //[SerializeField] private SMVariables _variables = new();
        //Note, make nonreliant on YellowPaper later.
        public AYellowpaper.SerializedCollections.SerializedDictionary<string, State> states;
        [HideInEditMode, DisableInPlayMode] public bool signalReady = true;
        public Queue<string> signalQueue = new();
        public Timer.OneTime signalQueueDecay = new(1f);
        public SerializedDictionary<string, UltEvent> globalSignals;

        #region Buttons

        [Button("Add New State")]
        protected override void AddChild()
        {
            var NSGO = new GameObject("NewState");
            NSGO.transform.parent = stateHolder.transform;
            NSGO.AddComponent<State>();
        }
        [Button(nameof(__enableSiblingCreation), ConditionResult.ShowHide)]
        protected override void AddSibling() { }

        #endregion Buttons


        #endregion

        #region Data

        public bool statesSetup { get => _statesSetup; private set => _statesSetup = value; }
        private bool _statesSetup;
        public Transform stateHolder { get => _stateHolder; private set => _stateHolder = value; }
        private Transform _stateHolder;
        public State currentState { get; private set; }
        //public SMVariables Variables => _variables;

        public System.Action waitforMachineInit;


        #endregion

        #region EditorData
        protected override bool __showSepFromChildren => false;
        protected override bool __enableSiblingCreation => false;

        protected override bool __isMachine => true;

        #endregion 



        #region Real Unity Messages

        protected virtual void Awake()
        {
            if (!statesSetup) Setup(this, this, -1);
            if (!Gameplay.Active) return;
            OnAwake();
            DoAwake();

            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnEnter(null, false);
            currentState = children[0].EnterState(null, true);

            waitforMachineInit?.Invoke();
        }

        private void Reset()
        {
            Transform tryRoot = transform.Find("States");
            if (tryRoot != null) this.NewGameObject("States", parent: transform);
        }

        protected virtual void Update()
        {
            if (signalQueueDecay.running) signalQueueDecay.Tick(() =>
            {
                if (signalQueue.Count > 0) signalQueue.Dequeue();
                if (signalQueue.Count > 0) signalQueueDecay.Begin();
            });
            DoUpdate();
        }

        protected virtual void FixedUpdate() => DoFixedUpdate();


        #endregion



        #region Initialization


        public new void Setup(StateMachine machine, State parent, int layer, bool makeDirty = false)
        {
            if (stateHolder == null)
            {
                Transform tryRoot = transform.Find("States");
                stateHolder = tryRoot != null ? tryRoot : throw new System.Exception("State Root Missing");
            }
            if (stateHolder.childCount == 0) 
                throw new System.Exception("Stateless State Machines are not supported. If you need to use StateBehaviors on something with only one state, create a dummy state.");

            //Variables.Initialize();

            this.machine = this;
            this.layer = -1;
            this.parent = this;
            active = true;

            lineage = new State[1] { this };

            this.OnSetup();

            {
                childCount = stateHolder.childCount;
                children = new State[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    children[i] = stateHolder.GetChild(i).GetComponent<State>();
                    children[i].Setup(machine, this, layer + 1);
                }
            }//Children Setup

            behaviors = GetComponents<StateBehavior>();
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].Setup(this);

            statesSetup = true;

#if UNITY_EDITOR
            if (makeDirty) EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        [Button]
        public void ManualSetup()
        {
            if (statesSetup)
            {
                bool answer = EditorUtility.DisplayDialog("Setup", "This State Machine has already been setup, do you still want to setup again?", "Yes", "No");
                if (!answer) return;
            }

            Setup(this, this, -1);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);

            EditorUtility.DisplayDialog("Setup Complete", "This State Machine has been setup, be sure to save changes to the prefab.", "Nice.");
        }
#endif


        protected virtual void OnSetup() { }
        protected virtual void OnAwake() { }

        #endregion


        public virtual void TransitionState(State nextState) => TransitionState(nextState, currentState);
        public virtual void TransitionState(State nextState, State prevState)
        {
            // Pre Checks
            if (
                !Application.isPlaying ||
                nextState == null ||
                nextState.locked ||
                nextState == currentState ||
                nextState == prevState ||
                prevState == null ||
                prevState == this ||
                !prevState.active
               ) return;


            int i = prevState.lineage.Length - 1;
            for (; i >= 0;)
            {
                prevState.lineage[i].ExitState(nextState);
                if (i == 0 || nextState.lineage.Contains(prevState.lineage[i - 1])) break;
                i--;
            }
            for (; i < nextState.lineage.Length - 1; i++)
                nextState.lineage[i].EnterState(prevState, false);
            currentState = nextState.EnterState(prevState);
        }
        

        public bool SendSignal(string name, bool addToQueue = true, bool overrideReady = false)
        {
            if (gameObject.activeSelf == false || Time.timeScale == 0 || PauseMenu.Active) return false;
            if ((signalReady || overrideReady) && EnactSignal(name)) return true;
            else if (addToQueue)
            {
                signalQueue.Enqueue(name);
                if (signalQueueDecay.length > 0) signalQueueDecay.Begin();
            }
            return false;
        }
        public bool SendSignalBasic(string name) => SendSignal(name);

        public void ReadySignal()
        {
            signalReady = true;
            while (signalQueue.Count > 0)
                if (EnactSignal(signalQueue.Dequeue()))
                    break;
        }

        private bool EnactSignal(string name)
        {
            if (currentState.signals.TryGetValue(name, out UltEvent resultEvent) || globalSignals.TryGetValue(name, out resultEvent))
            {
                resultEvent?.Invoke();
                return true;
            }
            return false;
        }

        public void FinishSignal() => SendSignal("Finish", addToQueue: false, overrideReady: true);


        /// <summary>
        /// A class tracking designer-defined variables kept across the whole StateMachine. <br />
        /// Basic and untested. Possibly not fully functional.<br />
        /// Available Types include: Bool, Int, Float, Vector2, Vector3, String, Char. <br />
        /// Probably not relevant.
        /// </summary>
        [Serializable]
        public class SMVariables
        {

            [Serializable]
            public struct VarEntry
            {
                public enum VariableType { Bool, Int, Float, Vector2, Vector3, String, Char }

                public string name;
                public VariableType type;
            }

            [SerializeField] VarEntry[] variables;
            private Dictionary<string, object> Vars;
            private bool init;

            /// <summary>
            /// Initializes the Variables. Should only be called by StateMachine.
            /// </summary>
            internal void Initialize()
            {
                if (init) return;
                Vars = new Dictionary<string, object>();
                for (int i = 0; i < variables.Length; i++)
                {
                    Type targetType = variables[i].type switch
                    {
                        VarEntry.VariableType.Bool => typeof(bool),
                        VarEntry.VariableType.Int => typeof(int),
                        VarEntry.VariableType.Float => typeof(float),
                        VarEntry.VariableType.Vector2 => typeof(Vector2),
                        VarEntry.VariableType.Vector3 => typeof(Vector3),
                        VarEntry.VariableType.String => typeof(string),
                        VarEntry.VariableType.Char => typeof(char),
                        _ => null
                    };
                    if (targetType == null) return;
                    Vars.Add(variables[i].name, Activator.CreateInstance(targetType));
                }
                init = true;
            }

            /// <summary>
            /// Sets the value of a variable.<br />Variable must exist and be of correct provided type.
            /// </summary>
            /// <typeparam name="T">The Type of the variable.</typeparam>
            /// <param name="key">The name of the variable.</param>
            /// <param name="value">The value you which to inflict.</param>
            public void SetValue<T>(string key, T value)
            {
                if (!Vars.ContainsKey(key) || Vars[key].GetType() != typeof(T)) return;
                Vars[key] = value;
            }
            /// <summary>
            /// Gets the value of a variable.<br />Will give default value if Variable doesn't exist or is wrong type.<br />Consider using Exists<T>(key) to check if the variable exists.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key"></param>
            /// <returns>Returns the value of the variable, or default if the variable doesn't exist.</returns>
            public T GetValue<T>(string key) => !Vars.ContainsKey(key) || Vars[key].GetType() != typeof(T) ? default : (T)Vars[key];
            /// <summary>
            /// Returns whether the Variable of type T with name key does exist.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key"></param>
            /// <returns>Returns true if the Variable does exist and is the right type, false otherwise.</returns>
            public bool Exists<T>(string key) => Vars.ContainsKey(key) && typeof(T) == Vars[key].GetType();

        }
    }
}

