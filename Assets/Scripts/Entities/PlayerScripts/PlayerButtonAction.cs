using System;
using System.Collections;
using UltEvents;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[System.Serializable]
public abstract class PlayerButtonAction : PolymorphicObject
{
    protected virtual void Begin()
    {
        PlayerController.CurrentPlayerButtonAction = this;
        StartRoutine();
    }
    protected virtual void Finish()
    {
        PlayerController.CurrentPlayerButtonAction = null;
        StopRoutine();
    }
    public abstract void Press();
    public abstract void Release();
    protected abstract IEnumerator HoldRoutine();
    protected CoroutinePlus coroutine;

    protected void StartRoutine() => CoroutinePlus.Begin(ref coroutine, HoldRoutine(), Player.Controller);
    protected void StopRoutine()
    {
        if (coroutine)
        {
            coroutine.StopAuto();
            coroutine = null;
        }
    }

    [System.Serializable]
    public class BasicPush : PlayerButtonAction
    {
        public UltEvent pressEvent;
        public UltEvent releaseEvent;
        public override void Press()
        {
            Begin();
            pressEvent?.Invoke();
        }
        public override void Release()
        {
            releaseEvent?.Invoke();
            Finish();
        }
        protected override IEnumerator HoldRoutine()
        { yield return null; }

    }
    [System.Serializable]
    public class TapOrHold : PlayerButtonAction
    {
        public UltEvent pressInstantEvent;
        public UltEvent releaseInstantEvent;
        public UltEvent tapEvent;
        public float holdTime = 0.3f;
        public UltEvent holdEvent;
        public bool autoFinishHold = true;
        private bool pastHold = false;
        public override void Press()
        {
            Begin();
            pressInstantEvent?.Invoke();
            pastHold = false;
        }
        public override void Release()
        {
            releaseInstantEvent?.Invoke();
            if (pastHold) holdEvent?.Invoke();
            else tapEvent?.Invoke();
            Finish();
        }

        protected override IEnumerator HoldRoutine()
        {
            yield return new WaitForSeconds(holdTime);
            pastHold = true;
            if (autoFinishHold) Release();
        }
    }
    [System.Serializable]
    public class TapHoldOrLongHold : PlayerButtonAction
    {
        // Fields matching original PlayerButtonActions
        public UltEvent pressEvent;
        public UltEvent releaseEvent;
        public UltEvent tapEvent;
        public float holdTime = 0.3f;
        public UltEvent holdEvent;
        public UltEvent holdReleaseEvent;
        public float longHoldTime = 1.2f;
        public UltEvent longHoldEvent;
        public bool autoFinishLongHold = true;

        // Delegate to be invoked on release to produce correct behavior (tap / hold-release / long-hold)
        private Action releaseResult = null;

        public override void Press()
        {
            // If any immediate events are configured, preserve event-driven behavior and do not lock-in.
            if (releaseEvent != null
                || tapEvent != null
                || holdEvent != null
                || holdReleaseEvent != null
                || longHoldEvent != null
                )
            {
                // still invoke the press event immediately
                pressEvent?.Invoke();
                return;
            }

            // Otherwise begin locked-in hold routine.
            Begin();
            pressEvent?.Invoke();
        }

        public override void Release()
        {
            releaseEvent?.Invoke();
            releaseResult?.Invoke();
            Finish();
        }

        protected override IEnumerator HoldRoutine()
        {
            float time = 0f;

            // Default release action is a tap unless overwritten by a hold or long-hold.
            releaseResult = () => tapEvent?.Invoke();

            // Handle normal hold threshold.
            if (holdEvent != null || holdReleaseEvent != null)
            {
                while (time < holdTime)
                {
                    time += Time.deltaTime;
                    yield return null;
                }

                holdEvent?.Invoke();
                releaseResult = () => holdReleaseEvent?.Invoke();
            }

            // Handle long-hold threshold.
            if (longHoldEvent != null)
            {
                while (time < longHoldTime)
                {
                    time += Time.deltaTime;
                    yield return null;
                }

                releaseResult = () => longHoldEvent?.Invoke();
                if (autoFinishLongHold) Release();
            }
        }
    }
    [System.Serializable]
    public class TargetDependant : PlayerButtonAction, ICustomDrawer
    {
        [SerializeReference] public PlayerButtonAction hasMeleeTarget;
        [SerializeReference] public PlayerButtonAction hasRangedTarget;
        [SerializeReference] public PlayerButtonAction noTarget;

        public PlayerButtonAction Choose() =>
            TargetingManager.MeleeChannel.CurrentTarget ? hasMeleeTarget
            : TargetingManager.RangedChannel.CurrentTarget ? hasRangedTarget
            : noTarget;

        protected override void Begin() => PlayerController.CurrentPlayerButtonAction = Choose();

        public override void Press() => Choose().Press();
        public override void Release() => Choose().Release();
        protected override IEnumerator HoldRoutine() => Choose().HoldRoutine();

        public VisualElement Draw(SerializedProperty p)
        {
            var root = new VisualElement();
#if UNITY_EDITOR

            TabView tabView = new();
            root.Add(tabView);
            tabView.reorderable = false;

            Tab meleeTab = new("Melee Target");
            Tab rangedTab = new("Ranged Target");
            Tab noneTab = new("No Target");

            tabView.Add(meleeTab);
            tabView.Add(rangedTab);
            tabView.Add(noneTab);

            meleeTab.Add(new Drawer().CreatePropertyGUI(p.FindPropertyRelative(nameof(hasMeleeTarget))));
            rangedTab.Add(new Drawer().CreatePropertyGUI(p.FindPropertyRelative(nameof(hasRangedTarget))));
            noneTab.Add(new Drawer().CreatePropertyGUI(p.FindPropertyRelative(nameof(noTarget))));
#endif
            return root;
        }
    }
}