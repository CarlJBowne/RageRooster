using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using UltEvents;

/// <summary>
/// Represents the behavior and lifecycle for a single input-style button attached to the player.
/// This class manages press/release semantics, short taps, hold events, and long-hold events using a
/// <see cref="CoroutinePlus"/> driven timing routine.
/// </summary>
/// <remarks>
/// - This script inherits from <see cref="PlayerStateBehavior"/> and is intended to be used within the player's state machine.
/// - Public fields are exposed for configuration in the Unity inspector (using <see cref="UltEvent"/> callbacks).
/// - The class "Locks itself In" when activated so that until ended no other state's definition of button behavior can begin. 
/// </remarks>
public class PlayerButtonAction : PlayerStateBehavior
{
    /// <summary>
    /// Logical button identifiers that instances of this class can represent.
    /// </summary>
    public enum Button
    {
        Jump,
        Attack,
        Grab,
        Charge,
        Aim,
        Parry,
    }

    // --- Inspector-configurable events and timing ---

    /// <summary>
    /// Invoked when the button is pressed (immediate press event).
    /// </summary>
    public UltEvent pressEvent;

    /// <summary>
    /// Invoked as soon as the button is released.
    /// </summary>
    public UltEvent releaseEvent;

    /// <summary>
    /// Invoked if the button was tapped (pressed and released quickly, i.e. did not reach hold thresholds).
    /// </summary>
    public UltEvent tapEvent;

    /// <summary>
    /// The duration (in seconds) the button must be held to count as a "hold".
    /// Default: 0.3 seconds.
    /// </summary>
    public float holdTime = 0.3f;

    /// <summary>
    /// Invoked when the hold threshold is first reached.
    /// </summary>
    public UltEvent holdEvent;

    /// <summary>
    /// Invoked when the button is released after having reached the hold threshold.
    /// </summary>
    public UltEvent holdReleaseEvent;

    /// <summary>
    /// The duration (in seconds) the button must be held to count as a "long hold".
    /// Default: 1.2 seconds.
    /// </summary>
    public float longHoldTime = 1.2f;

    /// <summary>
    /// Invoked when the long-hold action is performed.
    /// </summary>
    public UltEvent longHoldEvent;

    /// <summary>
    /// If true and <see cref="longHoldEvent"/> is configured, the long-hold will auto-complete the button lifecycle
    /// by calling <see cref="End"/> after invoking <see cref="longHoldEvent"/>. If false, the button remains locked
    /// until release and the <see cref="longHoldEvent"/> will be invoked on release (via <see cref="releaseResult"/>).
    /// </summary>
    public bool autoFinishLongHold = true;

    // --- Internal runtime state ---

    /// <summary>
    /// Delegate invoked when the button is released to perform the correct release-time behavior.
    /// Defaults to invoking <see cref="tapEvent"/> but may be switched to <see cref="holdReleaseEvent"/> or <see cref="longHoldEvent"/>
    /// depending on how long the button was held.
    /// </summary>
    private Action releaseResult = null;

    /// <summary>
    /// The coroutine wrapper running the hold/long-hold timing routine.
    /// </summary>
    private CoroutinePlus coroutine;

    // --- Public API ---

    /// <summary>
    /// Call when the button is pressed.
    /// Behavior:
    /// - If any of the immediate <see cref="UltEvent"/>s for press/release/tap/hold/long-hold are set, this method
    ///   will not force a lock-in (it returns early). This preserves the configured event driven behavior.
    /// - Otherwise it will call <see cref="LockIn"/> which starts the hold timing routine and "locks" the button state
    ///   until <see cref="End"/> or a long-hold auto-finishes it.
    /// </summary>
    public void Press()
    {
        if (releaseEvent != null
            || tapEvent != null
            || holdEvent != null
            || holdReleaseEvent != null
            || longHoldEvent != null
            ) return;

        LockIn();
    }

    /// <summary>
    /// Call when the button is released.
    /// - Immediately invokes <see cref="releaseEvent"/> if configured.
    /// - Invokes whichever release-time callback was chosen by the hold routine via <see cref="releaseResult"/>.
    /// - Ends the internal locked state via <see cref="End"/>.
    /// </summary>
    public void Release()
    {
        releaseEvent?.Invoke();
        releaseResult?.Invoke();
        End();
    }

    /// <summary>
    /// Begins the locked-in state and starts the internal hold timing routine.
    /// This constructs a new <see cref="CoroutinePlus"/> over <see cref="HoldRoutine"/> and gives it this player's
    /// controller as the owner so it runs automatically.
    /// </summary>
    /// <remarks>
    /// TODO: The original code includes comments to add this instance to the controller's button dictionary.
    /// That behavior remains commented to avoid assumptions about how the Controller manages buttons.
    /// </remarks>
    public void LockIn()
    {
        coroutine = new CoroutinePlus(HoldRoutine(), Machine.controller);
        //Add to Controller's button dictionary.
    }

    /// <summary>
    /// Ends the locked-in state and stops the internal hold timing routine.
    /// </summary>
    /// <remarks>
    /// Calling <see cref="StopAuto"/> on <see cref="CoroutinePlus"/> will stop automatic execution; the coroutine
    /// reference is then released so this object can be re-used.
    /// </remarks>
    public void End()
    {
        coroutine?.StopAuto();
        coroutine = null;
        //Remove from Controller's button dictionary.
    }

    /// <summary>
    /// Coroutine which tracks how long the button has been held and switches the release behavior accordingly.
    /// Sequence:
    /// 1. Set default <see cref="releaseResult"/> to call <see cref="tapEvent"/> (fire on quick release).
    /// 2. If hold-related events are configured, wait until <see cref="holdTime"/> then invoke <see cref="holdEvent"/>
    ///    and switch <see cref="releaseResult"/> to <see cref="holdReleaseEvent"/>.
    /// 3. If a <see cref="longHoldEvent"/> is configured, continue waiting until <see cref="longHoldTime"/>.
    ///    - If <see cref="autoFinishLongHold"/> is true, invoke <see cref="longHoldEvent"/> immediately and call <see cref="End"/>.
    ///    - Otherwise, switch <see cref="releaseResult"/> so the <see cref="longHoldEvent"/> occurs on release.
    /// </summary>
    /// <returns>An enumerator used by <see cref="CoroutinePlus"/>.</returns>
    IEnumerator HoldRoutine()
    {
        float time = 0f;
        // Default release action is a tap unless overwritten by a hold or long-hold.
        releaseResult = tapEvent.Invoke;

        // Handle normal hold threshold.
        if (holdEvent != null || holdReleaseEvent != null)
        {
            while (time < holdTime)
            {
                time += Time.deltaTime;
                yield return null;
            }

            holdEvent?.Invoke();
            releaseResult = holdReleaseEvent.Invoke;
        }

        // Handle long-hold threshold.
        if (longHoldEvent != null)
        {
            while (time < longHoldTime)
            {
                time += Time.deltaTime;
                yield return null;
            }

            if (autoFinishLongHold)
            {
                longHoldEvent?.Invoke();
                End();
            }
            else
            {
                // Defer invocation until release.
                releaseResult = longHoldEvent.Invoke;
            }
        }
    }
}