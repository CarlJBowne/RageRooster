using System;
using System.Collections.Generic;
using System.Text;
using SLS.GameStateMachine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RageRooster.Core
{
    //This Class is functionally split in two pieces: This, and Gameplay_Top.cs
    // Gameplay_Top.cs is the implementation of everything that needs top-level capabilities.
    public abstract class Gameplay : GameStateSingle<Gameplay>
    {
        public override bool Additive => false;
        public static GameObject[] rootObjects;
        [SerializeField] protected GameState titleScreenGameState;

        protected abstract void DoReloadSave(); public static void ReloadSave() => Get.DoReloadSave();

        /// <summary>
        /// Callback event for when a Save is about to be reloaded.
        /// </summary>
        public static event System.Action PreReloadSave; 
        protected static void InvokePreReloadSave() => PreReloadSave?.Invoke();
        /// <summary>
        /// A Callback event for when the Gameplay system updates, invoked in <see cref="Update"/>.
        /// </summary>
        public static event System.Action onUpdate;
        protected static void InvokeOnUpdate() => onUpdate?.Invoke();
        /// <summary>
        /// A Callback event for when the Gameplay system has finally finished its introduction.
        /// </summary>
        public static event System.Action onFinalAwake;
        protected static void InvokeOnFinalAwake() => onFinalAwake?.Invoke();
        /// <summary>
        /// A Callbck event for when the Gameplay system is Unloaded.
        /// </summary>
        public static event System.Action onDestroy;
        protected static void InvokeOnDestroy() => onDestroy?.Invoke();

        private const float bobSpeed = 1f;
        private const float rotateSpeed = 90f;
        protected void FixedUpdate()
        {
            float time = Time.time;
            float bob = Mathf.Sin(time * bobSpeed);
            float rotate = time * rotateSpeed;

            for (int i = 0; i < bobAndTurnList.Count; i++) bobAndTurnList[i].DoUpdate(bob, rotate);
        }
        public static List<BobAndTurn> bobAndTurnList = new();

        public static void BeginSaveFile(int index) => Get.DoBeginSaveFile(index);
        protected abstract void DoBeginSaveFile(int index);
        
        public static void EndGame() => Get.DoEndGame();
        protected abstract void DoEndGame();

    }
}
