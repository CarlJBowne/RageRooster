using System;
using System.Collections.Generic;
using System.Text;
using SLS.GameStateMachine;
using UnityEngine;

namespace RageRooster.Core
{
    //This Class is functionally split in two pieces: This, and GameplayToplevel.cs
    // GameplayToplevel is the implementation of everything that needs top-level capabilities.
    public abstract class Gameplay : GameStateSingle<Gameplay>
    {
        public override bool Additive => false;
        public static GameObject[] rootObjects;

        protected abstract void DoReloadSave(); public static void ReloadSave() => Get.DoReloadSave();


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


    }
}
