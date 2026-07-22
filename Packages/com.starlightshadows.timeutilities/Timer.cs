using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Simple, reusable timer. Default implementation that must be driven manually via Tick().
/// <br/> Has subclasses <see cref="Timer.Auto"/> and <see cref="Timer.MonoDriven"/> for alternate implementations.
/// </summary>
[System.Serializable]
public class Timer
{
    /// <summary>
    /// Duration of the timer in seconds. This is the only field intended to be visible in the Editor.
    /// Must be non-negative.
    /// </summary>
    public float length; //The only field that should actually be visible in Editor;

    /// <summary>
    /// Whether the timer should automatically restart after firing.
    /// </summary>
    public bool loop = true;

    /// <summary>
    /// Whether the timer uses unscaled time (ignores timeScale).
    /// </summary>
    public bool unscaled = true;

    // NonSerialized
    /// <summary>
    /// The action invoked when the timer completes a cycle. Not serialized.
    /// </summary>
    [NonSerialized] public Action action;

    /// <summary>
    /// The current elapsed time of the timer in seconds.
    /// </summary>
    public float time { get; private set; } = 0f;

    /// <summary>
    /// Whether the timer is currently active (ticking).
    /// </summary>
    public bool active { get; private set; } = false;

    /// <summary>
    /// Whether the timer is currently paused.
    /// </summary>
    public bool paused { get; private set; } = false;


    /// <summary>
    /// Create a timer instance.
    /// </summary>
    /// <param name="length">Duration in seconds. Values less than zero are clamped to zero.</param>
    /// <param name="loop">Whether the timer should loop after firing.</param>
    public Timer(float length, bool loop = false)
    {
        this.length = Mathf.Max(0f, length);
        this.loop = loop;
    }

    /// <summary>
    /// Start the timer and optionally set the action to invoke when the timer completes.
    /// </summary>
    /// <param name="targetAction">Action to invoke on completion. If null, existing action is preserved.</param>
    public virtual void Start(Action targetAction)
    {
        active = true;
        paused = false;
        if (targetAction != null) action = targetAction;
    }

    /// <summary>
    /// Stop the timer. The timer will no longer tick until started again.
    /// </summary>
    public virtual void Stop() => active = false;

    /// <summary>
    /// Pause or unpause the timer.
    /// </summary>
    /// <param name="unPause">If true, unpause; otherwise toggle paused state.</param>
    public virtual void Pause(bool unPause = false)
    {
        paused = !unPause;
    }

    /// <summary>
    /// Reset the elapsed time to zero. Does not change active/paused state.
    /// </summary>
    public virtual void Reset() => time = 0f;


    /// <summary>
    /// Advance the timer by the current frame's delta time. Returns true the frame the timer reaches its length.
    /// This is the method used by the background runner as well.
    /// </summary>
    /// <returns>True on the frame the timer completes a cycle and invokes its action; otherwise false.</returns>
    public virtual bool Tick()
    {
        if (!active || paused) return false;

        if (length > 0) time += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        if (time < length) return false;

        // fire
        action?.Invoke();

        if (loop) time %= length;
        else
        {
            time = 0;
            active = false;
        }

        return true;
    }


    /// <summary>
    /// Progress of the timer as a value from 0 to 1 (clamped). 0 = just started, 1 = reached or exceeded length.
    /// </summary>
    public float Progress => length <= 0f ? 1f : Mathf.Clamp01(time / length);

    /// <summary>
    /// Remaining time in seconds until the next firing (>= 0).
    /// </summary>
    public float Remaining => Mathf.Max(0f, length - time);


    /// <summary>
    /// Returns a short string representation of the timer.
    /// </summary>
    public override string ToString() =>
        $"Timer(len={length:0.00}, t={time:0.00}, active={active}, loop={loop})";


    /// <summary>
    /// A Timer subtype that automatically attaches itself to the background updater when started.
    /// </summary>
    public class Auto : Timer
    {
        /// <summary>
        /// Create an Auto timer.
        /// </summary>
        /// <param name="length">Duration in seconds.</param>
        /// <param name="loop">Whether to loop after firing.</param>
        public Auto(float length, bool loop = true) : base(length, loop) { }

        /// <summary>
        /// Start the timer and attach it to the background UpdateProxy so it will be ticked automatically.
        /// </summary>
        /// <param name="targetAction">Action to invoke on completion.</param>
        public override void Start(Action targetAction)
        {
            base.Start(targetAction);
            UpdateProxy.AttachTimer(this);
        }

        /// <summary>
        /// Stop the timer and detach it from the background UpdateProxy.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            UpdateProxy.DetachTimer(this);
        }
    }

    /// <summary>
    /// A Timer subtype designed to be driven by a MonoBehaviour via a coroutine.
    /// </summary>
    public class MonoDriven : Timer
    {
        /// <summary>
        /// Create a MonoDriven timer.
        /// </summary>
        /// <param name="length">Duration in seconds.</param>
        /// <param name="loop">Whether to loop after firing.</param>
        public MonoDriven(float length, bool loop = true) : base(length, loop) { }

        /// <summary>
        /// Start the timer and launch a coroutine on the provided MonoBehaviour to drive it.
        /// </summary>
        /// <param name="targetAction">Action to invoke on completion.</param>
        /// <param name="self">MonoBehaviour used to start the coroutine.</param>
        public void Start(Action targetAction, MonoBehaviour self)
        {
            base.Start(targetAction);
            Coroutine.Begin(ref coroutine, TickCoroutine(), self, false);
        }

        /// <summary>
        /// Stop the timer and stop the coroutine driving it (if any).
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            coroutine?.StopAuto();
        }

        Coroutine coroutine;

        /// <summary>
        /// Coroutine that advances the timer each frame until it completes or is stopped.
        /// </summary>
        /// <returns>IEnumerator for the Unity coroutine system.</returns>
        IEnumerator TickCoroutine()
        {
            time = 0;
            do
            {
                while (time < length)
                {
                    if (paused || !active)
                    {
                        yield return null;
                        continue;
                    }
                    Tick();
                    yield return null;
                }
            } while (loop);
        }

        /// <summary>
        /// THIS VERSION WILL NOT ALLOW THIS SUBTYPE TO FUNCTION, USE <see cref="Start(Action, MonoBehaviour)"/>.
        /// </summary>
        /// <param name="targetAction">Not used.</param>
        /// <exception cref="InvalidOperationException">Always thrown to prevent misuse of this overload.</exception>
        public override void Start(Action targetAction) => throw new InvalidOperationException();
    }



}
