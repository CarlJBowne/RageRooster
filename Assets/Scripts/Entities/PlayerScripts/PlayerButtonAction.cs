using System;
using System.Collections;
using UltEvents;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using RageRooster.Systems.SaveSystem;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[System.Serializable]
public abstract class PlayerButtonAction : PolymorphicObject
{
    public bool persistAcrossStateChange = false;

    public virtual void Begin(InputAction button)
    {
        if (active || Current != null) return;
        Current = this;
        active = true;
        activeButton = button;
        StartRoutine();
    }
    public virtual void Finish()
    {
        if (!active || Current == null || Current != this) return;
        Current = null;
        active = false;
        activeButton = null;
        StopRoutine();
    }

    public abstract void Press();
    public abstract void Release();
    protected abstract IEnumerator HoldRoutine();

    public void StartRoutine() => CoroutinePlus.Begin(ref coroutine, HoldRoutine(), Player.Controller);
    public void StopRoutine()
    {
        if (coroutine)
        {
            coroutine.StopAuto();
            coroutine = null;
        }
    }

    //Non Serialized
    public static PlayerButtonAction Current { get; protected set; } = null;
    public bool active { get; protected set; } = false;
    protected CoroutinePlus coroutine = null;
    public InputAction activeButton { get; protected set; } = null;

    [System.Serializable]
    public class BasicPush : PlayerButtonAction
    {
        public UltEvent pressEvent;
        public UltEvent releaseEvent;
        public override void Press()
        {
            pressEvent?.Invoke();
            if (releaseEvent != null) Finish();
        }
        public override void Release()
        {
            releaseEvent?.Invoke();
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
            pressInstantEvent?.Invoke();
            pastHold = false;
        }
        public override void Release()
        {
            releaseInstantEvent?.Invoke();
            if (pastHold) holdEvent?.Invoke();
            else tapEvent?.Invoke();
        }

        protected override IEnumerator HoldRoutine()
        {
            yield return new WaitForSeconds(holdTime);
            pastHold = true;
            if (autoFinishHold)
            {
                Release();
                Finish();
            }
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
            pressEvent?.Invoke();
        }

        public override void Release()
        {
            releaseEvent?.Invoke();
            releaseResult?.Invoke();
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
                if (autoFinishLongHold)
                {
                    Release();
                    Finish();
                }
            }
        }
    }
    [System.Serializable]
    public class CrossStatePressRelease : PlayerButtonAction
    {
        public UltEvent actionEvent;
        public PlayerButtonActions transferState;

        public override void Press()
        {
            var activeButton = this.activeButton;
            if (transferState != null) transferState.State.Enter();
            actionEvent?.Invoke();
            Finish();
            transferState.GetButtonAction(activeButton).Begin(activeButton);
        }
        public override void Release()
        {
            actionEvent?.Invoke();
            if (transferState != null) transferState.State.Enter();
            Finish();
        }
        protected override IEnumerator HoldRoutine() { yield return null; }
    }
    [System.Serializable]
    public class UpgradeDependant : PlayerButtonAction
    {
        [SerializeReference] public PlayerButtonAction hasUpgrade;
        [SerializeReference] public PlayerButtonAction noUpgrade;
        [SerializeField]
        Upgrades.Upgrade upgrade;
        public PlayerButtonAction Choose() => Upgrades.Active.HasUpgrade(upgrade) ? hasUpgrade : noUpgrade;
        protected PlayerButtonAction lockedAction;
        public override void Begin(InputAction button)
        {
            if (active || Current != null) return;
            lockedAction = Choose();
            active = true;
            activeButton = button;
            lockedAction.Begin(button);
        }
        public override void Finish()
        {
            if (!active || Current == null || Current != lockedAction) return;
            active = false;
            activeButton = null;
            lockedAction.Finish();
            lockedAction = null;
        }

        public override void Press() => lockedAction.Press();
        public override void Release() => lockedAction.Release();
        protected override IEnumerator HoldRoutine() => throw new NotImplementedException(); //Don't.

#if UNITY_EDITOR
        public override bool OverrideBody(VisualElement.Hierarchy container, SerializedProperty property)
        {
            container.Clear();

            PropertyField persistField = new(property.FindPropertyRelative(nameof(persistAcrossStateChange)));
            PropertyField upgradeField = new(property.FindPropertyRelative(nameof(upgrade)));

            container.Add(persistField);
            container.Add(upgradeField);

            var tabDrawer = new TabbedDrawer();
            container.Add(tabDrawer);

            tabDrawer.Add("Has Upgrade", property.FindPropertyRelative(nameof(hasUpgrade)));
            tabDrawer.Add("Lacks Upgrade", property.FindPropertyRelative(nameof(noUpgrade)));

            return true;
        }
#endif
    }
    [System.Serializable]
    public class TargetDependant : PlayerButtonAction
    {
        [SerializeReference] public PlayerButtonAction hasMeleeTarget;
        [SerializeReference] public PlayerButtonAction hasRangedTarget;
        [SerializeReference] public PlayerButtonAction noTarget;

        public PlayerButtonAction Choose() =>
            TargetingManager.MeleeChannel.CurrentTarget ? hasMeleeTarget
            : TargetingManager.RangedChannel.CurrentTarget ? hasRangedTarget
            : noTarget;

        protected Target lockedTarget;
        protected PlayerButtonAction lockedAction;

        public override void Begin(InputAction button)
        {
            if (active || Current != null) return;

            lockedAction = Choose();
            lockedTarget = TargetingManager.MeleeChannel.CurrentTarget != null
                ? TargetingManager.MeleeChannel.CurrentTarget
                : TargetingManager.RangedChannel.CurrentTarget != null
                    ? TargetingManager.RangedChannel.CurrentTarget
                    : null;

            active = true;
            activeButton = button;
            lockedAction.Begin(button);
        }
        public override void Finish()
        {
            if (!active || Current == null || Current != lockedAction) return;
            active = false;
            activeButton = null;
            lockedAction.Finish();
            lockedAction = null;
            lockedTarget = null;
        }


        public override void Press() => lockedAction.Press();
        public override void Release() => lockedAction.Release();
        protected override IEnumerator HoldRoutine() => throw new NotImplementedException(); //Don't.

#if UNITY_EDITOR
        public override bool OverrideBody(VisualElement.Hierarchy container, SerializedProperty property)
        {
            container.Clear();

            var root = new PolymorphicObject.TabbedDrawer();
            container.Add(root);

            root.Add("Melee Target", property.FindPropertyRelative(nameof(hasMeleeTarget)));
            root.Add("Ranged Target", property.FindPropertyRelative(nameof(hasRangedTarget)));
            root.Add("No Target", property.FindPropertyRelative(nameof(noTarget)));

            return true;
        }
#endif
    }




}