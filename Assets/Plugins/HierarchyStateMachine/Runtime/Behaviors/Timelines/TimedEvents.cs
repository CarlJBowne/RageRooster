using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ULT_EVENTS
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

namespace SLS.StateMachineH.Timelines
{
    public class TimedEvents : StateTimeline
    {
        public float loopLength;

        [System.Serializable]
        public struct TimedEvent
        {
            public float time;
            public EVENT output;
            [System.NonSerialized] public bool hasFired;
        }
        public List<TimedEvent> events;


        protected override void OnTick(float delta)
        {
            for (int i = 0; i < events.Count ; i++)
            {
                if (WasPointPassed(events[i].time))
                    events[i].output?.Invoke();
            }
            if(loopLength > 0f && elapsedTime >= loopLength) elapsedTime %= loopLength;
        }
    }
}