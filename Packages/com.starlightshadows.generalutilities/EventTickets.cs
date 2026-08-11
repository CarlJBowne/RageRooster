using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using UltEvents;
using UnityEngine.Events;

namespace SLS.GeneralUtilities.EventTickets
{
    public class EventTicket
    {
        public bool Subscribed { get; protected set; }

        readonly Action _adder;
        readonly Action _remover;

        public virtual void Subscribe()
        {
            if (Subscribed) return;
            _adder();
            Subscribed = true;
        }

        public virtual void UnSubscribe()
        {
            if (!Subscribed) return;
            _remover();
            Subscribed = false;
        }

        protected EventTicket() { }
        public EventTicket(Action publisher, Action subscriber, bool subscribeNow = true)
        {
            _adder = () => publisher += subscriber;
            _remover = () => publisher -= subscriber;

            if (subscribeNow) Subscribe();
        }

        public static EventTicket Action(Action publisher, Action subscriber, bool subscribeNow = true) => new(publisher, subscriber, subscribeNow);
        public static EventTicket Action<T>(Action<T> publisher, Action<T> subscriber, bool subscribeNow = true) => new EventTicket<Action<T>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);
        public static EventTicket Action<T, T1>(Action<T, T1> publisher, Action<T, T1> subscriber, bool subscribeNow = true) => new EventTicket<Action<T, T1>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);
        public static EventTicket Action<T, T1, T2>(Action<T, T1, T2> publisher, Action<T, T1, T2> subscriber, bool subscribeNow = true) => new EventTicket<Action<T, T1, T2>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);

        public static EventTicket<T> Manual<T>(Action<T> adder, Action<T> remover, T handler, bool subscribeNow = true) where T : System.Delegate => new(adder, remover, handler, subscribeNow);


        public static EventTicket<UnityAction> Unity(UnityEvent unityEvent, UnityAction handler, bool subscribeNow = true) => new(h => unityEvent.AddListener(h), h => unityEvent.RemoveListener(h), handler, subscribeNow);
        public static EventTicket<UnityAction<T>> Unity<T>(UnityEvent<T> unityEvent, UnityAction<T> handler, bool subscribeNow = true) where T : System.Delegate => new(h => unityEvent.AddListener(h), h => unityEvent.RemoveListener(h), handler, subscribeNow);

        public static EventTicket Ult(UltEvent ultEvent, Action handler, bool subscribeNow = true) => new(() => ultEvent.AddListener(handler), () => ultEvent.RemoveListener(handler), subscribeNow);
        public static EventTicket Ult<T>(UltEvent<T> ultEvent, Action<T> handler, bool subscribeNow = true) => new(() => ultEvent.AddListener(handler), () => ultEvent.RemoveListener(handler), subscribeNow);
        public static EventTicket Ult<T, T1>(UltEvent<T, T1> ultEvent, Action<T, T1> handler, bool subscribeNow = true) => new(() => ultEvent.AddListener(handler), () => ultEvent.RemoveListener(handler), subscribeNow);
        public static EventTicket Ult<T, T1, T2>(UltEvent<T, T1, T2> ultEvent, Action<T, T1, T2> handler, bool subscribeNow = true) => new(() => ultEvent.AddListener(handler), () => ultEvent.RemoveListener(handler), subscribeNow);
    }

    public class EventTicket<T> : EventTicket where T : System.Delegate
    {
        readonly Action<T> _adder;
        readonly Action<T> _remover;
        readonly T _handler;

        public override void Subscribe()
        {
            if (Subscribed) return;
            _adder(_handler);
            Subscribed = true;
        }

        public override void UnSubscribe()
        {
            if (!Subscribed) return;
            _remover(_handler);
            Subscribed = false;
        }

        // Optional: expose the handler if you need it
        public T Handler => _handler;

        public EventTicket(Action<T> adder, Action<T> remover, T handler, bool subscribeNow = true)
        {
            _adder = adder ?? throw new ArgumentNullException(nameof(adder));
            _remover = remover ?? throw new ArgumentNullException(nameof(remover));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));

            if (subscribeNow) Subscribe();
        }
    }

    public static class Xtensions_EventTickets
    {
        public static void SubscribeAll(this List<EventTicket> list)
        { for (int i = 0; i < list.Count; i++) list[i].Subscribe(); }
        public static void UnSubscribeAll(this List<EventTicket> list)
        { for (int i = 0; i < list.Count; i++) list[i].UnSubscribe(); }

        public static EventTicket Subscribe(this Action publisher, Action subscriber, bool subscribeNow = true) => new(publisher, subscriber, subscribeNow);
        public static EventTicket Subscribe<T>(this Action<T> publisher, Action<T> subscriber, bool subscribeNow = true) => new EventTicket<Action<T>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);
        public static EventTicket Subscribe<T, T1>(this Action<T, T1> publisher, Action<T, T1> subscriber, bool subscribeNow = true) => new EventTicket<Action<T, T1>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);
        public static EventTicket Subscribe<T, T1, T2>(this Action<T, T1, T2> publisher, Action<T, T1, T2> subscriber, bool subscribeNow = true) => new EventTicket<Action<T, T1, T2>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);

        public static EventTicket<T> SubscribeTo<T>(this T handler, Action<T> adder, Action<T> remover, bool subscribeNow = true) where T : System.Delegate => new(adder, remover, handler, subscribeNow);


        public static EventTicket<UnityAction> Subscribe(this UnityEvent unityEvent, UnityAction handler, bool subscribeNow = true) => new(h => unityEvent.AddListener(h), h => unityEvent.RemoveListener(h), handler, subscribeNow);
        public static EventTicket<UnityAction<T>> Subscribe<T>(this UnityEvent<T> unityEvent, UnityAction<T> handler, bool subscribeNow = true) where T : System.Delegate => new(h => unityEvent.AddListener(h), h => unityEvent.RemoveListener(h), handler, subscribeNow);

        public static EventTicket Subscribe(this UltEvent ultEvent, Action handler, bool subscribeNow = true) => new(() => ultEvent.AddListener(handler), () => ultEvent.RemoveListener(handler), subscribeNow);
        public static EventTicket Subscribe<T>(this UltEvent<T> ultEvent, Action<T> handler, bool subscribeNow = true) => new(() => ultEvent.AddListener(handler), () => ultEvent.RemoveListener(handler), subscribeNow);
        public static EventTicket Subscribe<T, T1>(this UltEvent<T, T1> ultEvent, Action<T, T1> handler, bool subscribeNow = true) => new(() => ultEvent.AddListener(handler), () => ultEvent.RemoveListener(handler), subscribeNow);
        public static EventTicket Subscribe<T, T1, T2>(this UltEvent<T, T1, T2> ultEvent, Action<T, T1, T2> handler, bool subscribeNow = true) => new(() => ultEvent.AddListener(handler), () => ultEvent.RemoveListener(handler), subscribeNow);

    }

    // Example usage:
    // var t = EventTicketHelpers.FromEvent<Action<int>>(h => publisher.SomeEvent += h, h => publisher.SomeEvent -= h, (i) => Debug.Log(i));
    // t.UnSubscribe();

    // UnityEvent:
    // var ut = EventTicketHelpers.FromUnityEvent(myUnityEvent, new UnityAction(MyHandler));
    // ut.UnSubscribe();
}
