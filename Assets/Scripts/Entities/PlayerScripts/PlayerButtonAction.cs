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
public abstract class PlayerButtonAction : Polymorph
{
    public bool persistAcrossStateChange = false;

    /// <summary>
    /// Call when this Action has officially begun: locks in this Action as the currently active one.
    /// </summary>
    /// <param name="button">Pass in the InputAction that triggered this so that it can be stored and compared. </param>
    public virtual void Begin()
    {
        if (active || Current != null) return;
        Current = this;
        active = true;
        StartRoutine();
    }
    /// <summary>
    /// Call when this Action has officially ended: releases this Action from being currently active. (Also runs when Cancelled.)
    /// </summary>
    public virtual void Finish()
    {
        if (!active || Current == null || Current != this) return;
        Current = null;
        active = false;
        StopRoutine();
    }

    /// <summary>
    /// The actual functionality tied to when the button tied to this Action is pressed. Separate from <see cref="Begin"/>, Most basic Action types should run <see cref="Begin"/> at the beginning of this.
    /// </summary>
    public abstract void Press();
    /// <summary>
    /// The actual functionality tied to when the button tied to this Action is released. Separate from <see cref="Finish"/>, Most basic Action types should run <see cref="Finish"/> at the end of this.
    /// </summary>
    public abstract void Release();
    /// <summary>
    /// The psudeo-Coroutine that runs for the duration of the Action.
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerator HoldRoutine();

    public void StartRoutine() => CoroutinePlus.Begin(ref ActiveRoutine, HoldRoutine(), Player.Controller);
    public void StopRoutine()
    {
        if (ActiveRoutine)
        {
            ActiveRoutine.StopAuto();
            ActiveRoutine = null;
        }
    }

    //Non Serialized
    public static PlayerButtonAction Current { get; protected set; } = null;
    public bool active { get; protected set; } = false;
    protected static CoroutinePlus ActiveRoutine = null;










    [System.Serializable]
    public class BasicPush : PlayerButtonAction
    {
        public UltEvent pressEvent;
        public UltEvent releaseEvent;
        public override void Press()
        {
            if (releaseEvent.HasCalls) Begin();
            pressEvent?.Invoke();
        }
        public override void Release()
        {
            releaseEvent?.Invoke();
            if (releaseEvent.HasCalls) Finish();
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

            Begin();

            // Otherwise begin locked-in hold routine.
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
                if (autoFinishLongHold)
                {
                    Release();
                    Finish();
                }
            }
        }
    }

    [System.Serializable]
    public abstract class Base_ChooseType : PlayerButtonAction
    {
        public abstract PlayerButtonAction Choose();
        public override void Press()
        {
            PlayerButtonAction chosen = Choose();
            if (chosen == null) return;
            chosen.Press();
        }
        public sealed override void Release() => throw new InvalidOperationException();
        public sealed override void Begin() => throw new InvalidOperationException();
        public sealed override void Finish() => throw new InvalidOperationException();
        protected sealed override IEnumerator HoldRoutine() => throw new InvalidOperationException();
    }

    [System.Serializable]
    public class UpgradeDependant : Base_ChooseType
    {
        [SerializeReference] public PlayerButtonAction hasUpgrade;
        [SerializeReference] public PlayerButtonAction noUpgrade;
        [SerializeField] Upgrades.Upgrade upgrade;
        public override PlayerButtonAction Choose() => Upgrades.Active.HasUpgrade(upgrade) ? hasUpgrade : noUpgrade;

#if UNITY_EDITOR
        public override bool OverrideBody(VisualElement container, SerializedProperty property)
        {
            container.hierarchy.Clear();

            PropertyField persistField = new(property.FindPropertyRelative(nameof(persistAcrossStateChange)));
            PropertyField upgradeField = new(property.FindPropertyRelative(nameof(upgrade)));

            container.Add(persistField);
            container.Add(upgradeField);

            TabbedDrawer tabDrawer = new();
            container.Add(tabDrawer);

            tabDrawer.Add("Has Upgrade", property.FindPropertyRelative(nameof(hasUpgrade)));
            tabDrawer.Add("Lacks Upgrade", property.FindPropertyRelative(nameof(noUpgrade)));

            return true;
        }
#endif
    }
    [System.Serializable]
    public class TargetDependant : Base_ChooseType
    {
        [SerializeReference] public PlayerButtonAction hasMeleeTarget;
        [SerializeReference] public PlayerButtonAction hasRangedTarget;
        [SerializeReference] public PlayerButtonAction noTarget;

        public override PlayerButtonAction Choose() =>
            TargetingManager.MeleeChannel.CurrentTarget ? hasMeleeTarget
            : TargetingManager.RangedChannel.CurrentTarget ? hasRangedTarget
            : noTarget;

#if UNITY_EDITOR
        public override bool OverrideBody(VisualElement container, SerializedProperty property)
        {
            container.hierarchy.Clear();

            var root = new Polymorph.TabbedDrawer();
            container.Add(root);

            root.Add("Melee Target", property.FindPropertyRelative(nameof(hasMeleeTarget)));
            root.Add("Ranged Target", property.FindPropertyRelative(nameof(hasRangedTarget)));
            root.Add("No Target", property.FindPropertyRelative(nameof(noTarget)));

            return true;
        }
#endif
    }

    [System.Serializable]
    public class CrossStatePressRelease : PlayerButtonAction
    {
        public UltEvent actionEvent;
        public PlayerButtonActions transferState;

        public override void Press()
        {
            if (transferState != null) transferState.State.Enter();
            actionEvent?.Invoke();
            transferState[PlayerController.ActiveButtonAction].Begin();
        }
        public override void Release()
        {
            actionEvent?.Invoke();
            if (transferState != null) transferState.State.Enter();
            Finish();
        }
        protected override IEnumerator HoldRoutine() { yield return null; }
    }


}