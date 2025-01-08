using UnityEngine;
using EditorAttributes;

namespace SLS.StateMachineV3
{
    [RequireComponent(typeof(State))]
    public class StateAnimator : StateBehavior
    {

        #region Config

        private enum EntryAnimAction { None, Play, CrossFade, Trigger }

        [SerializeField] EntryAnimAction onEntry;
        [SerializeField, ShowField(nameof(__showOnEnterName))] string onEnterName;
        [SerializeField, ShowField(nameof(__showOnEnterTime))] float onEnterTime;
        

        #endregion
        #region Data
        [HideInInspector] public Animator animator;
        #endregion


        public override void OnAwake()
        {
            if (TryGetComponentFromMachine(out animator) == false) Destroy(this);
        }

        public override void OnEnter(State prev)
        {
            if (onEntry == EntryAnimAction.Play) Play(onEnterName);
            if (onEntry == EntryAnimAction.CrossFade) CrossFade(onEnterName, onEnterTime);
            if (onEntry == EntryAnimAction.Trigger) Trigger(onEnterName);
        }

        public void Play(string name) => animator.Play(name);
        public void CrossFade(string name, float time = 0f) => animator.CrossFade(name, time);
        public void Trigger(string name) => animator.SetTrigger(name);




        #region Edtior
        private bool __showOnEnterName => onEntry != EntryAnimAction.None;
        private bool __showOnEnterTime => onEntry == EntryAnimAction.CrossFade;

        #endregion
    }
}
